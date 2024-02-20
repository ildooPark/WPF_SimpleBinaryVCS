using MemoryPack;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using WinForms = System.Windows.Forms;

namespace SimpleBinaryVCS.DataComponent
{
    public class VersionControlManager : IModel
    {
        public string? CurrentProjectPath {  get; set; }
        public Action<object>? ResetAction;
        public Action<object>? UpdateAction;
        public Action<object>? PullAction;
        public Action<object>? FetchAction;
        public Action<object>? ProjectLoaded;
        public Action<object>? ProjectInitialized;

        public Action? VersionCheckFinished;
        private ProjectRepository? projectRepository; 
        public ProjectRepository ProjectRepository
        {
            get => projectRepository; 
            private set
            {
                projectRepository = value;
                mainProjectData = value.ProjectMain;
                NewestProjectData = value.ProjectDataList.First();
            }
        }

        private ProjectData? mainProjectData; 
        public ProjectData MainProjectData 
        { 
            get => mainProjectData ??= new ProjectData();
            set
            {
                if (projectRepository == null) throw new ArgumentNullException(nameof(projectRepository));
                projectRepository.ProjectMain = value;
                mainProjectData = projectRepository.ProjectMain;
                ProjectLoaded?.Invoke(mainProjectData);
            }
        }

        public ProjectData? NewestProjectData { get; private set; }
        private FileManager fileManager;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public VersionControlManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        public void Awake()
        {
            fileManager = App.FileManager;
        }

        #region Setup Project 
        public bool TryRetrieveProject(string projectPath)
        {
            string projectRepoBin;

            CurrentProjectPath = projectPath;
            string[] binFiles = Directory.GetFiles(CurrentProjectPath, "VersionLog.*", SearchOption.AllDirectories);

            if (binFiles.Length > 0)
            {
                projectRepoBin = binFiles[0];
                ProjectRepository? loadedProjectRepository;
                try
                {
                    var stream = File.ReadAllBytes(projectRepoBin);
                    loadedProjectRepository = MemoryPackSerializer.Deserialize<ProjectRepository>(stream);
                    if (loadedProjectRepository != null)
                    {
                        ProjectRepository = loadedProjectRepository;
                        MainProjectData = loadedProjectRepository.ProjectMain;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    return false;
                }
                FetchAction?.Invoke(projectPath);
                return true;
            }
            else
            {
                return false;
            }
        }
        public void InitializeProject(string projectFilePath)
        {
            // Project Repository Setup 
            ProjectRepository newProjectRepo = new ProjectRepository(projectFilePath, Path.GetFileName(projectFilePath)); 
            List<ProjectData> changedList = new List<ProjectData>();
            StringBuilder changeLog = new StringBuilder();
            TryGetAllFiles(projectFilePath, out string[]? newProjectFiles);
            if (newProjectFiles == null)
            { MessageBox.Show("Couldn't Get Project Files");  return; }
            ProjectData newProjectData = new ProjectData();
            newProjectData.UpdatedVersion = GetProjectVersionName(newProjectData);

            foreach (string filePath in newProjectFiles)
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(filePath);
                ProjectFile newFile = new ProjectFile
                    (
                    true,
                    new FileInfo(filePath).Length,
                    Path.GetFileName(filePath),
                    projectFilePath,
                    Path.GetRelativePath(projectFilePath, filePath),
                    fileInfo.FileVersion,
                    DataChangedState.Added
                    );

                newFile.DataHash = HashTool.GetFileMD5CheckSum(projectFilePath, filePath);
                newFile.DeployedProjectVersion = newProjectData.UpdatedVersion;
                newProjectData.ProjectFiles.Add(newFile);

                newProjectData.ChangedFiles.Add(newFile);
                changeLog.AppendLine($"Added {newFile.DataName}");
            }

            newProjectData.UpdatedTime = DateTime.Now;
            newProjectData.ChangeLog = changeLog.ToString();
            newProjectData.NumberOfChanges = newProjectData.ProjectFiles.Count;
            newProjectData.ProjectName = Path.GetFileName(projectFilePath);
            byte[] serializedFile = MemoryPackSerializer.Serialize(MainProjectData);
            File.WriteAllBytes($"{MainProjectData.ProjectPath}\\VersionLog.bin", serializedFile);
            ProjectInitialized?.Invoke(MainProjectData);
            
        }
        #endregion
        #region Version Management Tools

        private string GetProjectVersionName(ProjectData projData)
        {
            if (NewestProjectData == null || 
                NewestProjectData.UpdatedVersion == null || 
                ProjectRepository.UpdateCount == 0)
            {
                return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_{++ProjectRepository.UpdateCount}";
            }
            return $"{HashTool.GetUniqueComputerID(Environment.MachineName)}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{++ProjectRepository.UpdateCount}";
        }
        
        /// <summary>
        /// Merging Src => Dst, Occurs in version Reversion or Merging from outer source. 
        /// </summary>
        /// <param name="srcData"></param>
        /// <param name="dstData"></param>
        /// <param name="isRevert"> True if Reverting, else (Merge) false.</param>
        public List<ProjectFile>? FindVersionDifferences(ProjectData srcData, ProjectData dstData, bool isRevert = true)
        {
            try
            {
                //StringBuilder fileIntegrityLog = new StringBuilder();
                //fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {MainProjectData.UpdatedVersion}");
                List <ProjectFile> diffLog = new List<ProjectFile> ();
                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();
                Dictionary<string, ProjectFile> srcDict = srcData.ProjectFilesDict;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFilesDict;
                
                // Files which is not on the Dst 
                IEnumerable<string> filesToAdd = srcData.ProjectRelFilePathsList.Except(dstData.ProjectRelFilePathsList);
                // Files which is not on the Src
                IEnumerable<string> filesToDelete = dstData.ProjectRelFilePathsList.Except(srcData.ProjectRelFilePathsList);
                // Directories which is not on the Src
                IEnumerable<string> dirsToAdd = srcData.ProjectRelDirsList.Except(dstData.ProjectRelDirsList);
                // Directories which is not on the Dst
                IEnumerable<string> dirsToDelete = dstData.ProjectRelDirsList.Except(srcData.ProjectRelDirsList);
                // Files to Overwrite
                IEnumerable<string> intersectFiles = srcData.ProjectRelFilePathsList.Intersect(dstData.ProjectRelFilePathsList);

                //1. Directories 
                foreach (string dirRelPath in dirsToAdd)
                {
                    if (dirRelPath == "VersionLog.bin") continue;
                    //fileIntegrityLog.AppendLine($"{dirRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(MainProjectData.ProjectPath, dirRelPath);
                    ProjectFile file = new ProjectFile(MainProjectData.ProjectPath, dirRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    diffLog.Add(file);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    //fileIntegrityLog.AppendLine($"{dirRelPath} has been Deleted");
                    ProjectFile file = dstDict[dirRelPath];
                    file.DataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    diffLog.Add(file);
                }
                //2. Files 
                foreach (string fileRelPath in filesToAdd)
                {
                    if (fileRelPath == "VersionLog.bin") continue;
                    //fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(MainProjectData.ProjectPath, fileRelPath);
                    ProjectFile file = new ProjectFile(MainProjectData.ProjectPath, fileRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    diffLog.Add(file);
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    //fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile file = dstDict[fileRelPath];
                    file.DataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    diffLog.Add(file);
                }
                //3. File Overwrite
                foreach (string fileRelPath in intersectFiles)
                {
                    if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
                    {
                        //fileIntegrityLog.AppendLine($"File {dstDict[fileRelPath].DataName} on {fileRelPath} has been modified");
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(MainProjectData.ProjectPath, fileRelPath));

                        ProjectFile file = new ProjectFile(dstDict[fileRelPath]);
                        file.IsNew = true;
                        file.DataState = DataChangedState.Modified | DataChangedState.IntegrityChecked;
                        file.DataHash = srcDict[fileRelPath].DataHash;
                        file.UpdatedTime = new FileInfo(file.DataAbsPath).LastAccessTime;
                        diffLog.Add(file);
                    }
                }
                //fileIntegrityLog.AppendLine("Integrity Check Complete");
                return diffLog;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run File Integrity Check");
                return null; 
            }
        }
        public void FindVersionDifferences(ProjectData srcData, ProjectData dstData, ObservableCollection<ProjectFile> changeList)
        {
            if (srcData == null  || dstData == null)
            {
                MessageBox.Show($"One or more project is set to null");
                return;
            }
            try
            {
                StringBuilder fileIntegrityLog = new StringBuilder();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {MainProjectData.UpdatedVersion}");
                Dictionary<string, ProjectFile> srcDict = srcData.ProjectFilesDict;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFilesDict;

                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();
                // Fitting to the SrcFiles, 
                // Files which is not on the Dst 
                IEnumerable<string> filesToAdd = srcData.ProjectRelFilePathsList.Except(dstData.ProjectRelFilePathsList);
                // Files which is not on the Src
                IEnumerable<string> filesToDelete = dstData.ProjectRelFilePathsList.Except(srcData.ProjectRelFilePathsList);
                // Directories which is not on the Src
                IEnumerable<string> dirsToAdd = srcData.ProjectRelDirsList.Except(dstData.ProjectRelDirsList);
                // Directories which is not on the Dst
                IEnumerable<string> dirsToDelete = dstData.ProjectRelDirsList.Except(srcData.ProjectRelDirsList);
                // Files to Overwrite
                IEnumerable<string> intersectFiles = srcData.ProjectRelFilePathsList.Intersect(dstData.ProjectRelFilePathsList);

                //1. Directories 
                foreach (string dirRelPath in dirsToAdd)
                {
                    if (dirRelPath == "VersionLog.bin") continue;
                    fileIntegrityLog.AppendLine($"{dirRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(MainProjectData.ProjectPath, dirRelPath);
                    ProjectFile file = new ProjectFile(MainProjectData.ProjectPath, dirRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    dstData.ChangedFiles.Add(file);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    fileIntegrityLog.AppendLine($"{dirRelPath} has been Deleted");
                    ProjectFile file = new ProjectFile(dstDict[dirRelPath]);
                    file.DataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    changeList.Add(file);
                }

                foreach (string fileRelPath in filesToAdd)
                {
                    if (fileRelPath == "VersionLog.bin") continue;
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(MainProjectData.ProjectPath, fileRelPath);
                    ProjectFile file = new ProjectFile(MainProjectData.ProjectPath, fileRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    dstData.ChangedFiles.Add(file);
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile file = new ProjectFile(dstDict[fileRelPath]);
                    file.DataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    dstData.ChangedFiles.Add(file);
                }
                
                foreach (string fileRelPath in intersectFiles)
                {
                    if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
                    {
                        fileIntegrityLog.AppendLine($"File {dstDict[fileRelPath].DataName} on {fileRelPath} has been modified");
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(MainProjectData.ProjectPath, fileRelPath));

                        ProjectFile file = new ProjectFile(dstDict[fileRelPath]);
                        file.DataState = DataChangedState.Modified | DataChangedState.IntegrityChecked;
                        file.DataHash = srcDict[fileRelPath].DataHash;
                        file.IsNew = true;
                        file.UpdatedTime = new FileInfo(file.DataAbsPath).LastAccessTime;
                        dstData.ChangedFiles.Add(file);
                    }
                }

                fileIntegrityLog.AppendLine("Integrity Check Complete");
                VersionCheckFinished?.Invoke();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run File Integrity Check");
            }
        }
        #endregion
        /// <summary>
        /// Preceded by the backup of the current Project
        /// </summary>
        /// <param name="obj"></param>
        private void UponUpdateRequest(object obj)
        {
            // 0. Generate New Project
            ProjectData newProjectData = new ProjectData(ProjectRepository.ProjectMain);
            // 1. Check for backup on the Current version, if none found, make one. 

            // 2. Make Physical changes to the files 
            IList<ProjectFile> changedList = fileManager.ChangedFileList.ToList();

            // 3. Make Update, and backup for new version. 

            // 4. Call for new Fetch Action 

        }
        private void TryGetAllFiles(string directoryPath, out string[]? files)
        {
            try
            {
                files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                files = null;
            }
        }
        private void OnReset(object obj)
        {

        }

        #region Planned
        #region Exports
        /// <summary>
        /// Input: Requested Project Data 
        /// Output: All the project files, including projectData meta file
        /// in a @.projectParentDir/Exports/ProjectVersion
        /// </summary>
        /// <param name="projectData"></param>
        public void ExportProject(ProjectData projectData)
        {
            // Requests for all the registerd project files, 
            // Copy paste to the 
        }
        public void ExportProjectRepo(ProjectRepository projectRepository)
        {

        }
        #endregion
        #endregion
    }
}