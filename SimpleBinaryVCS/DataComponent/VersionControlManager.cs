using MemoryPack;
using SimpleBinaryVCS.Model;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using WinForms = System.Windows.Forms;

namespace SimpleBinaryVCS.DataComponent
{
    public class VersionControlManager
    {
        public string? currentProjectPath {  get; set; }
        public Action<object>? updateAction;
        public Action<object>? revertAction;
        public Action<object>? pullAction;
        public Action<object>? fetchAction;

        public Action<object>? projectLoaded;
        public Action<object>? projectInitialized;

        public Action? versionCheckFinished;
        private ProjectRepository projectRepository; 
        public ProjectRepository ProjectRepository
        {
            get => projectRepository; 
            private set
            {
                projectRepository = value;
                NewestProjectData = value.ProjectDataList.First();
                CurrentProjectData = value.ProjectMain; 
            }
        }
        private ProjectData? currentProjectData; 
        public ProjectData CurrentProjectData 
        { 
            get => currentProjectData ?? new ProjectData();
            set
            {
                currentProjectData = value;
                projectLoaded?.Invoke(currentProjectData);
            }
        }
        public ProjectData? NewestProjectData { get; private set; }
        public ObservableCollection<ProjectData> ProjectDataList { get; private set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public VersionControlManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            ProjectDataList = new ObservableCollection<ProjectData>();
        }

        #region Binary Comparision Through MD5 CheckSum
        /// <summary>
        /// Returns true if content is the same. 
        /// </summary>
        /// <param name="srcFile"></param>
        /// <param name="dstFile"></param>
        /// <param name="result">First is srcHash, Second is dstHash</param>
        /// <returns></returns>
        public bool TryCompareMD5CheckSum(string? srcFile, string? dstFile, out (string?, string?) result)
        {
            if (srcFile == null || dstFile == null)
            {
                result = (null, null); 
                return false;
            }
            byte[] srcHashBytes, dstHashBytes;
            using MD5 md5 = MD5.Create();
            if (md5 == null)
            {
                MessageBox.Show("Failed to Initialize MD5");
                result = (null, null);  
                return false; 
            }
            using (var srcStream = File.OpenRead(srcFile))
            {
                srcHashBytes = md5.ComputeHash(srcStream);
            }
            using (var dstStream = File.OpenRead(dstFile))
            {
                dstHashBytes = md5.ComputeHash(dstStream);
            }
            string srcHashString = BitConverter.ToString(srcHashBytes).Replace("-", ""); 
            string dstHashString = BitConverter.ToString(srcHashBytes).Replace("-", "");
            result =  (srcHashString, dstHashString);
            return srcHashString == dstHashString;
        }
        public string GetFileMD5CheckSum(string projectPath, string srcFileRelPath)
        {
            byte[] srcHashBytes;
            string srcFileFullPath = Path.Combine(projectPath, srcFileRelPath);
            using MD5 md5 = MD5.Create();
            if (md5 == null)
            {
                MessageBox.Show($"Failed to Initialize MD5 for file {srcFileRelPath}");
                return ""; 
            }
            using (var srcStream = File.OpenRead(srcFileFullPath))
            {
                srcHashBytes = md5.ComputeHash(srcStream);
            }
            md5.Dispose(); 
            return BitConverter.ToString(srcHashBytes).Replace("-", "");
        }
        public async Task GetFileMD5CheckSumAsync(TrackedData file)
        {
            try
            {
                byte[] srcHashBytes;
                using MD5 md5 = MD5.Create();
                if (md5 == null)
                {
                    MessageBox.Show("Failed to Initialize MD5 Async");
                    return;
                }
                using (var srcStream = File.OpenRead(file.DataAbsPath))
                {
                    srcHashBytes = await md5.ComputeHashAsync(srcStream);
                }
                string resultHash = BitConverter.ToString(srcHashBytes).Replace("-", "");
                file.DataHash = resultHash;
                md5.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured {ex.Message} \nwhile Computing hash async by this file {file.DataName}");
            }
        }
        public async Task<string?> GetFileMD5CheckSumAsync(string fileFullPath)
        {
            try
            {
                byte[] srcHashBytes;
                using MD5 md5 = MD5.Create();
                if (md5 == null)
                {
                    MessageBox.Show("Failed to Initialize MD5 Async");
                    return null;
                }
                using (var srcStream = File.OpenRead(fileFullPath))
                {
                    srcHashBytes = await md5.ComputeHashAsync(srcStream);
                }
                string resultHash = BitConverter.ToString(srcHashBytes).Replace("-", "");
                md5.Dispose();
                return resultHash;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured {ex.Message} \nwhile Computing hash async by this file {Path.GetFileName(fileFullPath)}");
                return null;
            }
        }
        #endregion
        #region Setup Project 
        public bool TryRetrieveProject(string projectPath)
        {
            var openFD = new WinForms.FolderBrowserDialog();
            string projectDataBin;
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                currentProjectPath = openFD.SelectedPath;
            }
            else return false;
            openFD.Dispose();
            //Get .bin VersionLog File 
            string[] binFiles = Directory.GetFiles(ProjectRepository.ProjectPath, "VersionLog.*", SearchOption.AllDirectories);

            if (binFiles.Length > 0)
            {
                projectDataBin = binFiles[0];
                ProjectRepository? projectRepo;
                try
                {
                    var stream = File.ReadAllBytes(projectDataBin);
                    projectRepo = MemoryPackSerializer.Deserialize<ProjectRepository>(stream);
                    if (projectRepo != null)
                    {
                        CurrentProjectData = projectRepo.ProjectMain;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                fetchAction?.Invoke(projectPath);
            }
            else
            {
                var result = MessageBox.Show("VersionLog file not found!\n Initialize A New Project?",
                    "Import Project", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    InitializeProject(openFD.SelectedPath);
                    projectInitialized?.Invoke(CurrentProjectData);
                }
                else
                {
                    MessageBox.Show("Please Select Another Project Path");
                    return false;
                }
            }
            return true; 
        }
        private void InitializeProject(string projectFilePath)
        {
            // Project Repository Setup 
            ProjectRepository newProjectRepo = new ProjectRepository(projectFilePath, Path.GetFileName(projectFilePath)); 

            StringBuilder changeLog = new StringBuilder();
            TryGetAllFiles(projectFilePath, out string[]? newProjectFiles);
            if (newProjectFiles == null)
            { MessageBox.Show("Couldn't Get Project Files");  return; }
            ProjectData newProjectData = new ProjectData();
            newProjectData.UpdatedVersion = GetprojectDataVersionName();

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

                newFile.DataHash = GetFileMD5CheckSum(projectFilePath, filePath);
                newFile.DeployedProjectVersion = newProjectData.UpdatedVersion;
                newProjectData.ProjectFiles.Add(newFile);
                newProjectData.ChangedFiles.Add(newFile);
                changeLog.AppendLine($"Added {newFile.DataName}");
            }

            newProjectData.UpdatedTime = DateTime.Now;
            newProjectData.ChangeLog = changeLog.ToString();
            newProjectData.NumberOfChanges = newProjectData.ProjectFiles.Count;
            newProjectData.ProjectName = Path.GetFileName(projectFilePath);
            byte[] serializedFile = MemoryPackSerializer.Serialize(CurrentProjectData);
            File.WriteAllBytes($"{CurrentProjectData.ProjectPath}\\VersionLog.bin", serializedFile);
            updateAction?.Invoke(newProjectData);
        }
        #endregion
        #region Version Management Tools

        private string GetprojectDataVersionName()
        {
            if (NewestProjectData == null || 
                NewestProjectData.UpdatedVersion == null || 
                ProjectRepository.UpdateCount == 0)
            {
                return $"{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_{++ProjectRepository.UpdateCount}";
            }
            return $"{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{++ProjectRepository.UpdateCount}";
        }
        /// <summary>
        /// Merging Src => Dst, Occurs in version Reversion or Merging from outer source. 
        /// </summary>
        /// <param name="srcData"></param>
        /// <param name="dstData"></param>
        /// <param name="isRevert"> True if Reverting, else (Merge) false.</param>
        public void CompareVersion(ProjectData srcData, ProjectData dstData, bool isRevert = true)
        {
            try
            {
                StringBuilder fileIntegrityLog = new StringBuilder();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {CurrentProjectData.UpdatedVersion}");
                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();

                // 
                // Fitting to the SrcFiles, 
                // Files which is not on the Dst 
                IEnumerable<string> filesToAdd = srcData.ProjectFileRelPaths().Except(dstData.ProjectFileRelPaths());
                // Files which is not on the Src
                IEnumerable<string> filesToDelete = dstData.ProjectFileRelPaths().Except(srcData.ProjectFileRelPaths());
                // Directories which is not on the Src
                IEnumerable<string> dirsToAdd = srcData.ProjectRelDirs().Except(dstData.ProjectRelDirs());
                IEnumerable<string> dirsToDelete = dstData.ProjectRelDirs().Except(srcData.ProjectRelDirs());
                // Files to Overwrite
                IEnumerable<string> intersectFiles = srcData.ProjectFileRelPaths().Intersect(dstData.ProjectFileRelPaths());

                //1. Directories 
                foreach (string dirRelPath in dirsToAdd)
                {
                    if (dirRelPath == "VersionLog.bin") continue;
                    fileIntegrityLog.AppendLine($"{dirRelPath} has been Added");
                    string? fileHash = GetFileMD5CheckSum(CurrentProjectData.ProjectPath, dirRelPath);
                    ProjectFile file = new ProjectFile(CurrentProjectData.ProjectPath, dirRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    changedFileList.Add(file);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    fileIntegrityLog.AppendLine($"{dirRelPath} has been Deleted");
                    ProjectFile file = projectFilesDict[dirRelPath];
                    file.DataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    changedFileList.Add(file);
                }
                //2. Files 
                foreach (string fileRelPath in filesToAdd)
                {
                    if (fileRelPath == "VersionLog.bin") continue;
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = GetFileMD5CheckSum(CurrentProjectData.ProjectPath, fileRelPath);
                    ProjectFile file = new ProjectFile(CurrentProjectData.ProjectPath, fileRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    changedFileList.Add(file);
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile file = projectFilesDict[fileRelPath];
                    file.dataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    changedFileList.Add(file);
                }
                //3. File Overwrite

                foreach (string fileRelPath in intersectFiles)
                {
                    string? fileHash = GetFileMD5CheckSum(ProjectData.projectPath, fileRelPath);
                    if (projectFilesDict[fileRelPath].fileHash != fileHash)
                    {
                        fileIntegrityLog.AppendLine($"File {projectFilesDict[fileRelPath].fileName} on {fileRelPath} has been modified");
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(CurrentProjectData.ProjectPath, fileRelPath));

                        ProjectFile file = new ProjectFile(projectFilesDict[fileRelPath]);
                        file.dataState = DataChangedState.Modified | DataChangedState.IntegrityChecked;
                        file.dataHash = fileHash;
                        file.IsNew = true;
                        file.UpdatedTime = new FileInfo(file.fileFullPath()).LastAccessTime;
                        changedFileList.Add(file);
                    }
                }
                fileIntegrityLog.AppendLine("Integrity Check Complete");
                versionCheckFinished?.Invoke();
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

            // 1. Make Physical changes to the files 
            // 2. Make 
            // 3. Call for new Fetch Action 
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