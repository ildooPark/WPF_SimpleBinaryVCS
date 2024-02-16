using MemoryPack;
using SimpleBinaryVCS.Model;
using WinForms = System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SimpleBinaryVCS.DataComponent
{
    public class VersionControlManager
    {
        public string? mainProjectPath {  get; set; }
        public Action<object>? updateAction;
        public Action<object>? revertAction;
        public Action<object>? pullAction;
        public Action<ProjectData>? projectLoadAction;
        public Action<object>? fetchAction;
        public Action? versionCheckFinished;
        private ProjectRepository projectRepository; 
        public ProjectRepository ProjectRepository
        {
            get => projectRepository; 
            private set => projectRepository = value;
        }
        private ProjectData? currentProjectData; 
        public ProjectData CurrentProjectData 
        { 
            get => currentProjectData ?? new ProjectData();
            set
            {
                currentProjectData = value;
                projectLoadAction?.Invoke(currentProjectData);
            }
        }
        public ProjectData? NewestProjectData { get; private set; }
        public ObservableCollection<ProjectData> ProjectDataList { get; private set; }
        public VersionControlManager()
        {
            currentProjectData = new ProjectData();
            ProjectDataList = new ObservableCollection<ProjectData>();
        }

        #region MD5 CheckSum
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
        public async Task GetFileMD5CheckSumAsync(TrackedFile file)
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
                using (var srcStream = File.OpenRead(file.FileAbsPath))
                {
                    srcHashBytes = await md5.ComputeHashAsync(srcStream);
                }
                string resultHash = BitConverter.ToString(srcHashBytes).Replace("-", "");
                file.FileHash = resultHash;
                md5.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured {ex.Message} \nwhile Computing hash async by this file {file.FileName}");
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
        public bool TryRetrieveProject(string projectPath)
        {
            var openFD = new WinForms.FolderBrowserDialog();
            string projectDataBin;
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                mainProjectPath = openFD.SelectedPath;
                ProjectRepository.ProjectPath = openFD.SelectedPath;
                ProjectRepository.ProjectName = Path.GetFileName(openFD.SelectedPath);
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
                    FileChangedState.Added
                    );

                newFile.FileHash = GetFileMD5CheckSum(projectFilePath, filePath);
                newFile.DeployedProjectVersion = newProjectData.UpdatedVersion;
                newProjectData.ProjectFiles.Add(newFile);
                newProjectData.ChangedFiles.Add(newFile);
                changeLog.AppendLine($"Added {newFile.fileName}");
            }
            newProjectData.UpdatedTime = DateTime.Now;
            newProjectData.ChangeLog = changeLog.ToString();
            newProjectData.NumberOfChanges = newProjectData.ProjectFiles.Count;
            newProjectData.ProjectName = Path.GetFileName(projectFilePath);
            byte[] serializedFile = MemoryPackSerializer.Serialize(CurrentProjectData);
            File.WriteAllBytes($"{CurrentProjectData.ProjectPath}\\VersionLog.bin", serializedFile);
            ProjectName = CurrentProjectData.ProjectName;
            CurrentVersion = CurrentProjectData.UpdatedVersion;
            vcsManager.updateAction?.Invoke(projectFilePath);
        }
        private string GetprojectDataVersionName()
        {
            if (NewestProjectData == null || 
                NewestProjectData.UpdatedVersion == null || 
                ProjectRepository.RevisionNumber == 0)
            {
                return $"{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_{++ProjectRepository.RevisionNumber}";
            }
            return $"{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{++ProjectRepository.RevisionNumber}";
        }
        public void CompareVersion(ProjectData srcData,  ProjectData dstData)
        {
            try
            {
                StringBuilder fileIntegrityLog = new StringBuilder();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {CurrentProjectData.UpdatedVersion}");
                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();
                
                // Sort files, 
                // Fitting to the SrcFiles, 
                // Files which is not on the Dst 
                IEnumerable<string> filesToAdd = directoryFiles.Except(recordedFiles);
                // Files which is not on the Src
                IEnumerable<string> filesToDelete = recordedFiles.Except(directoryFiles);
                // Directories which is not on the Src
                IEnumerable<string> deletedDirs = recordedFiles.Except(directoryFiles);
                // Files to Overwrite
                IEnumerable<string> intersectFiles = recordedFiles.Intersect(directoryFiles);

                foreach (string fileRelPath in newlyAddedFiles)
                {
                    if (fileRelPath == "VersionLog.bin") continue;
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = GetFileMD5CheckSum(CurrentProjectData.ProjectPath, fileRelPath);
                    ProjectFile file = new ProjectFile(CurrentProjectData.ProjectPath, fileRelPath, fileHash, FileChangedState.Added | FileChangedState.IntegrityChecked);
                    changedFileList.Add(file);
                }

                foreach (string fileRelPath in deletedFiles)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile file = projectFilesDict[fileRelPath];
                    file.fileState = FileChangedState.Deleted | FileChangedState.IntegrityChecked;
                    changedFileList.Add(file);
                }
                foreach (string fileRelPath in intersectFiles)
                {
                    string? fileHash = vcsManager.GetFileMD5CheckSum(vcsManager.ProjectData.projectPath, fileRelPath);
                    if (projectFilesDict[fileRelPath].fileHash != fileHash)
                    {
                        fileIntegrityLog.AppendLine($"File {projectFilesDict[fileRelPath].fileName} on {fileRelPath} has been modified");
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(CurrentProjectData.ProjectPath, fileRelPath));

                        ProjectFile file = new ProjectFile(projectFilesDict[fileRelPath]);
                        file.fileState = FileChangedState.Modified | FileChangedState.IntegrityChecked;
                        file.fileHash = fileHash;
                        file.IsNew = true;
                        file.UpdatedTime = new FileInfo(file.fileFullPath()).LastAccessTime;
                        changedFileList.Add(file);
                    }
                }
                fileIntegrityLog.AppendLine("Integrity Check Complete");
                versionCheckFinished?.Invoke();
            }
            catch (Exception Ex)
            {
                System.Windows.MessageBox.Show($"{Ex.Message}. Couldn't Run File Integrity Check");
            }
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
    }
}