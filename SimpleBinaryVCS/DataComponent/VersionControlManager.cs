using SimpleBinaryVCS.Model;
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
        private ProjectData? projectData; 
        public ProjectData ProjectData 
        { 
            get => projectData ?? new ProjectData();
            set
            {
                projectData = value;
                projectLoadAction?.Invoke(projectData);
            }
        }
        public ProjectData? NewestProjectData { get; set; }
        public ObservableCollection<ProjectData> ProjectDataList { get; set; }
        public VersionControlManager()
        {
            projectData = new ProjectData();
            ProjectDataList = new ObservableCollection<ProjectData>();
        }

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
        public async Task GetFileMD5CheckSumAsync(ChangedFile file)
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

        public void CompareVersion(ProjectData srcData,  ProjectData dstData)
        {
            try
            {
                StringBuilder fileIntegrityLog = new StringBuilder();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {ProjectData.updatedVersion}");
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
                    string? fileHash = GetFileMD5CheckSum(ProjectData.projectPath, fileRelPath);
                    ProjectFile file = new ProjectFile(ProjectData.projectPath, fileRelPath, fileHash, FileChangedState.Added | FileChangedState.IntegrityChecked);
                    changedFileList.Add(file);
                }

                foreach (string fileRelPath in deletedFiles)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile file = projectFilesDict[fileRelPath];
                    file.fileChangedState = FileChangedState.Deleted | FileChangedState.IntegrityChecked;
                    changedFileList.Add(file);
                }
                foreach (string fileRelPath in intersectFiles)
                {
                    string? fileHash = vcsManager.GetFileMD5CheckSum(vcsManager.ProjectData.projectPath, fileRelPath);
                    if (projectFilesDict[fileRelPath].fileHash != fileHash)
                    {
                        fileIntegrityLog.AppendLine($"File {projectFilesDict[fileRelPath].fileName} on {fileRelPath} has been modified");
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(ProjectData.projectPath, fileRelPath));

                        ProjectFile file = new ProjectFile(projectFilesDict[fileRelPath]);
                        file.fileChangedState = FileChangedState.Modified | FileChangedState.IntegrityChecked;
                        file.fileHash = fileHash;
                        file.isNew = true;
                        file.updatedTime = new FileInfo(file.fileFullPath()).LastAccessTime;
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
    }
}