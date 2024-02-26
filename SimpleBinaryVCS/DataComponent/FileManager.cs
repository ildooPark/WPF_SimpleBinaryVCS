using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Diagnostics;
using System.IO;
using System.Text;
using WPF = System.Windows;

namespace SimpleBinaryVCS.DataComponent
{
    [Flags]
    public enum DataState
    {
        None = 0,
        Added = 1,
        Deleted = 1 << 1,
        Restored = 1 << 2,
        Modified = 1 << 3,
        PreStaged = 1 << 4,
        IntegrityChecked = 1 << 5,
        Backup = 1 << 6
    }
    public class FileManager : IManager
    {
        public event Action<object>? DataStagedEventHandler;
        public event Action<object>? DataPreStagedEventHandler;
        public event Action<object>? SrcProjectDataEventHandler;
        public event Action<object>? PreStagedDataOverlapEventHandler;
        public event Action<object, string, List<ProjectFile>>? IntegrityCheckEventHandler;

        private Dictionary<string, ProjectFile> backupFiles;
        private Dictionary<string, ProjectFile> projectFilesDict;
        private Dictionary<string, ProjectFile> preStagedFilesDict;
        private Dictionary<string, ChangedFile> registeredChangesDict; 
        private SemaphoreSlim asyncControl;

        private ProjectData? currentProjectData;
        public ProjectData? CurrentProjectData
        {
            get
            {
                if (currentProjectData == null)
                {
                    MessageBox.Show("Project Data is unreachable from FileManager");
                    return null;
                }
                return currentProjectData;
            }
        }
        private bool hasIntegrityIssue; 
        public bool HasIntegrityIssue
        {
            get => hasIntegrityIssue;
            set
            {
                hasIntegrityIssue = value;
            }
        }
        private FileHandlerTool fileHandlerTool;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public FileManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            projectFilesDict = new Dictionary<string, ProjectFile>();
            preStagedFilesDict = new Dictionary<string, ProjectFile>();
            registeredChangesDict = new Dictionary<string, ChangedFile>();
            fileHandlerTool = new FileHandlerTool();
            asyncControl = new SemaphoreSlim(5);
        }
        public void Awake()
        {
        }
        #region Identifying file differences against given version(s)
        public void MainProjectIntegrityCheck(object sender)
        {
            ProjectData? mainProject = currentProjectData;
            if (mainProject == null)
            {
                MessageBox.Show("Main Project is Missing");
                return;
            }
            preStagedFilesDict.Clear();
            try
            {
                StringBuilder fileIntegrityLog = new StringBuilder();
                List<ChangedFile> fileChanges = new List<ChangedFile>();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {mainProject.UpdatedVersion}");

                Dictionary<string, ProjectFile> projectFilesDict = mainProject.ProjectFiles;

                List<string> recordedFiles = mainProject.ProjectRelFilePathsList;
                List<string> recordedDirs = mainProject.ProjectRelDirsList;

                List<string> directoryFiles = new List<string>();
                List<string> directoryDirs = new List<string>();

                string[]? rawFiles = Directory.GetFiles(mainProject.ProjectPath, "*", SearchOption.AllDirectories);
                foreach (string absPathFile in rawFiles)
                {
                    directoryFiles.Add(Path.GetRelativePath(mainProject.ProjectPath, absPathFile));
                }

                string[]? rawDirs = Directory.GetDirectories(mainProject.ProjectPath, "*", SearchOption.AllDirectories);
                foreach (string absPathFile in rawDirs)
                {
                    directoryDirs.Add(Path.GetRelativePath(mainProject.ProjectPath, absPathFile));
                }

                IEnumerable<string> addedFiles = directoryFiles.Except(recordedFiles);
                IEnumerable<string> addedDirs = directoryDirs.Except(recordedDirs);
                IEnumerable<string> deletedFiles = recordedFiles.Except(directoryFiles);
                IEnumerable<string> deletedDirs = recordedDirs.Except(directoryDirs);
                IEnumerable<string> intersectFiles = recordedFiles.Intersect(directoryFiles);

                foreach (string dirRelPath in addedDirs)
                {
                    ProjectFile dstFile = new ProjectFile(mainProject.ProjectPath, dirRelPath, null, DataState.Added | DataState.IntegrityChecked, ProjectDataType.Directory);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Added | DataState.IntegrityChecked);
                    registeredChangesDict.Add(dstFile.DataRelPath, newChange);
                    preStagedFilesDict.Add(dirRelPath, dstFile);
                }

                foreach (string dirRelPath in deletedDirs)
                {
                    ProjectFile dstFile = new ProjectFile(mainProject.ProjectPath, dirRelPath, null, DataState.Deleted | DataState.IntegrityChecked, ProjectDataType.Directory);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Deleted | DataState.IntegrityChecked);
                    preStagedFilesDict.Add(dirRelPath, dstFile);
                    registeredChangesDict.Add(dstFile.DataRelPath, newChange);
                }

                foreach (string fileRelPath in addedFiles)
                {
                    if (fileRelPath == "ProjectMetaData.bin") continue;
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(mainProject.ProjectPath, fileRelPath);
                    ProjectFile dstFile = new ProjectFile(mainProject.ProjectPath, fileRelPath, fileHash, DataState.Added | DataState.IntegrityChecked, ProjectDataType.Directory);
                    preStagedFilesDict.Add(fileRelPath, dstFile);
                    registeredChangesDict.Add(dstFile.DataRelPath, new ChangedFile(dstFile, DataState.Added | DataState.IntegrityChecked));
                }

                foreach (string fileRelPath in deletedFiles)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile srcFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.None);
                    ProjectFile dstFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.Deleted | DataState.IntegrityChecked);
                    dstFile.UpdatedTime = DateTime.Now;
                    preStagedFilesDict.Add(fileRelPath, dstFile);
                    registeredChangesDict.Add(dstFile.DataRelPath, new ChangedFile(srcFile, dstFile, DataState.Deleted | DataState.IntegrityChecked, true));
                }

                foreach (string fileRelPath in intersectFiles)
                {
                    string? dirFileHash = HashTool.GetFileMD5CheckSum(mainProject.ProjectPath, fileRelPath);
                    if (projectFilesDict[fileRelPath].DataHash != dirFileHash)
                    {
                        fileIntegrityLog.AppendLine($"File {projectFilesDict[fileRelPath].DataName} on {fileRelPath} has been modified");

                        ProjectFile srcFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.None);
                        ProjectFile dstFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.Modified | DataState.IntegrityChecked);
                        dstFile.DataHash = dirFileHash;
                        dstFile.UpdatedTime = new FileInfo(srcFile.DataAbsPath).LastAccessTime;
                        preStagedFilesDict.Add(fileRelPath, dstFile);
                        registeredChangesDict.Add(dstFile.DataRelPath, new ChangedFile(srcFile, dstFile, DataState.Modified | DataState.IntegrityChecked, true));
                    }
                }

                fileIntegrityLog.AppendLine("Integrity Check Complete");
                DataStagedEventHandler?.Invoke(registeredChangesDict.Values.ToList());
                IntegrityCheckEventHandler?.Invoke(sender, fileIntegrityLog.ToString(), preStagedFilesDict.Values.ToList());
                if (preStagedFilesDict.Count > 0)
                    HasIntegrityIssue = false;
                else 
                    HasIntegrityIssue = true; 
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run File Integrity Check");
            }
        }

        /// <summary>
        /// Merging Src => Dst, Occurs in version Reversion or Merging from outer source. 
        /// </summary>
        /// <param name="srcData"></param>
        /// <param name="dstData"></param>
        /// <param name="isRevert"> True if Reverting, else (Merge) false.</param>
        public List<ChangedFile>? FindVersionDifferences(ProjectData srcData, ProjectData dstData, bool isProjectRevert)
        {
            if (!isProjectRevert)
                return FindVersionDifferences(srcData, dstData);
            try
            {
                List<ChangedFile> fileChanges = new List<ChangedFile>();
                
                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();

                Dictionary<string, ProjectFile> srcDict = srcData.ProjectFiles;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFiles;

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
                    ProjectFile dstFile = new ProjectFile(srcDict[dirRelPath], DataState.Added, dstData.ProjectPath);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Added);
                    fileChanges.Add(newChange);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    ProjectFile dstFile = new ProjectFile(dstDict[dirRelPath], DataState.Deleted);
                    fileChanges.Add(new ChangedFile(dstFile, DataState.Deleted));
                }

                foreach (string fileRelPath in filesToAdd)
                {
                    if (!backupFiles.TryGetValue(srcDict[fileRelPath].DataHash, out ProjectFile? backupFile))
                    {
                        MessageBox.Show($"Following Previous Project Version {srcData.UpdatedVersion}\n" +
                            $"Lacks Backup File {srcDict[fileRelPath].DataName}");
                        return null;
                    }
                    ProjectFile srcFile = new ProjectFile(backupFile, DataState.Backup, backupFile.DataSrcPath);
                    ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Restored);
                    fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Restored, true));
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Deleted);
                    fileChanges.Add(new ChangedFile(dstFile, DataState.Deleted));
                }

                foreach (string fileRelPath in intersectFiles)
                {
                    if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
                    {
                        if (!backupFiles.TryGetValue(srcDict[fileRelPath].DataHash, out ProjectFile? backupFile))
                        {
                            MessageBox.Show($"Following Previous Project Version {srcData.UpdatedVersion} Lacks Backup File {srcDict[fileRelPath].DataName}");
                            return null;
                        }
                        ProjectFile srcFile = new ProjectFile(backupFile, DataState.Backup, backupFile.DataSrcPath);
                        ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Restored);
                        fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Restored, true));
                    }
                }
                List<ProjectFile> changedListDst = new List<ProjectFile>();
                foreach (ChangedFile changedFile in fileChanges)
                {
                    if (changedFile.DstFile != null)
                    {
                        changedListDst.Add(new ProjectFile(changedFile.DstFile));
                    }
                }
                DataStagedEventHandler?.Invoke(fileChanges);
                return fileChanges;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run Find Version Differences For Backup");
                return null;
            }
        }
        public List<ChangedFile>? FindVersionDifferences(ProjectData srcData, ProjectData dstData)
        {
            try
            {
                List<ChangedFile> fileChanges = new List<ChangedFile>();

                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();

                Dictionary<string, ProjectFile> srcDict = preStagedFilesDict;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFiles;

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
                    ProjectFile dstFile = new ProjectFile(srcDict[dirRelPath], DataState.Added, dstData.ProjectPath);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Added);
                    fileChanges.Add(newChange);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    ProjectFile dstFile = new ProjectFile(dstDict[dirRelPath], DataState.Deleted);
                    fileChanges.Add(new ChangedFile(dstFile, DataState.Deleted));
                }

                foreach (string fileRelPath in filesToAdd)
                {
                    ProjectFile srcFile = new ProjectFile(srcDict[fileRelPath], DataState.None);
                    ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Added);
                    fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Added, true));
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Deleted);
                    fileChanges.Add(new ChangedFile(dstFile, DataState.Deleted));
                }

                foreach (string fileRelPath in intersectFiles)
                {
                    if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
                    {
                        ProjectFile srcFile = new ProjectFile(srcDict[fileRelPath], DataState.None);
                        ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Modified);
                        fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Modified, true));
                    }
                }
                DataStagedEventHandler?.Invoke(fileChanges);
                return fileChanges;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run Find Version Differences Against Given Src");
                return null;
            }
        }
        #endregion
        #region Calls For File Check
        public bool RetrieveDataSrc(string srcPath)
        {
            try
            {
                string[] binFiles = Directory.GetFiles(srcPath, "VersionLog.*", SearchOption.AllDirectories);
                if (binFiles.Length == 1)
                {
                    var stream = File.ReadAllBytes(binFiles[0]);
                    bool result = fileHandlerTool.TryDeserializeProjectData(binFiles[0], out ProjectData? srcProjectData);
                    if (srcProjectData != null)
                    {
                        srcProjectData.ProjectPath = srcPath;
                        srcProjectData.SetProjectFilesSrcPath();
                        RegisterNewData(srcProjectData);
                        DataPreStagedEventHandler?.Invoke(preStagedFilesDict.Values.ToList());
                    }
                }
                else
                {
                    RegisterNewData(srcPath);
                }
                DataPreStagedEventHandler?.Invoke(preStagedFilesDict.Values.ToList());
                return true; 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File Manager RetrieveDataSrc Error: {ex.Message}");
                return false;
            }

        }
        public void RegisterNewData(string updateDirPath)
        {
            string[]? filesFullPaths;
            string[]? dirsFullPaths;
            try
            {
                filesFullPaths = Directory.GetFiles(updateDirPath, "*", SearchOption.AllDirectories);
                dirsFullPaths = Directory.GetDirectories(updateDirPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"FileManager RegisterNewData Error {ex.Message}");
                filesFullPaths = null;
                dirsFullPaths = null;
            }
            if (filesFullPaths == null || dirsFullPaths == null)
            {
                WPF.MessageBox.Show($"Couldn't get files or dirrectories from given Directory {updateDirPath}");
                return;
            }

            try
            {
                foreach (string fileAbsPath in filesFullPaths)
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        new FileInfo(fileAbsPath).Length,
                        FileVersionInfo.GetVersionInfo(fileAbsPath).FileVersion,
                        Path.GetFileName(fileAbsPath),
                        updateDirPath,
                        Path.GetRelativePath(updateDirPath, fileAbsPath)
                        );
                    if (!preStagedFilesDict.TryAdd(newFile.DataRelPath, newFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {newFile.DataName}: for Update");
                    }
                    else continue;
                }
                foreach (string dirAbsPath in dirsFullPaths)
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        Path.GetFileName(dirAbsPath),
                        updateDirPath,
                        Path.GetRelativePath(updateDirPath, dirAbsPath)
                        );
                    if (!preStagedFilesDict.TryAdd(newFile.DataRelPath, newFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {newFile.DataName}: for Update");
                    }
                    else continue;
                }
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"FileManager RegisterNewData Error {ex.Message}");
                return;
            }
        }
        public void RegisterNewData(ProjectData srcProjectData)
        {
            try
            {
                foreach (ProjectFile srcFile in srcProjectData.ProjectFiles.Values)
                {
                    srcFile.DataState |= DataState.PreStaged;
                    if (!preStagedFilesDict.TryAdd(srcFile.DataRelPath, srcFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {srcFile.DataName}: for Update");
                    }
                    else continue;
                }

            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show(ex.Message);
                return;
            }
        }

        public async void StageNewFilesAsync()
        {
            await HashPreStagedFilesAsync();
            UpdateStageFileList();
        }

        public void RegisterNewfile(ProjectFile projectFile, DataState fileState)
        {
            ProjectFile newfile = new ProjectFile(projectFile, fileState | DataState.PreStaged);
            if (!preStagedFilesDict.TryAdd(newfile.DataRelPath, newfile))
            {
                PreStagedDataOverlapEventHandler?.Invoke(newfile);
                return;
            }
            DataPreStagedEventHandler?.Invoke(preStagedFilesDict.Values.ToList());
        }

        #endregion
        #region Update File Model
        private async Task HashPreStagedFilesAsync()
        {
            try
            {
                if (preStagedFilesDict.Count <= 0) return;
                List<Task> asyncTasks = new List<Task>();
                //Update changedFilesDict
                foreach (ProjectFile file in preStagedFilesDict.Values)
                {
                    if (file.DataType == ProjectDataType.Directory || file.DataHash != "") continue;
                    asyncTasks.Add(Task.Run(async () =>
                    {
                        await asyncControl.WaitAsync();
                        try
                        {
                            await HashTool.GetFileMD5CheckSumAsync(file);
                        }
                        finally
                        {
                            asyncControl.Release();
                        }
                    }));
                }
                await Task.WhenAll(asyncTasks);
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"File Manager UpdateHashFromChangedList Error: {ex.Message}");
            }
        }
        private void UpdateStageFileList()
        {
            if (preStagedFilesDict.Count <= 0) return;
            foreach (ProjectFile registerdFile in preStagedFilesDict.Values)
            {
                
                registerdFile.DataState &= ~DataState.PreStaged;
                if ((registerdFile.DataState & DataState.Restored) != 0 && registerdFile.DataType != ProjectDataType.Directory)
                {
                    backupFiles.TryGetValue(registerdFile.DataHash, out ProjectFile? backupFile);
                    if (backupFile != null)
                    {
                        ProjectFile srcFile = new ProjectFile(registerdFile, DataState.Backup, backupFile.DataSrcPath);
                        ProjectFile dstFile = new ProjectFile(registerdFile, DataState.Modified);
                        ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataState.Modified, true);
                        registeredChangesDict.TryAdd(registerdFile.DataRelPath, newChange);
                    }
                    //Reset SrcPath to BackupPath
                }
                if ((registerdFile.DataState & DataState.Deleted) != 0)
                {
                    ChangedFile newChange = new ChangedFile(new ProjectFile(registerdFile), DataState.Deleted);
                    registeredChangesDict.TryAdd(registerdFile.DataRelPath, newChange);
                }
                //compare the hash value, and if its the same, request to remove that file. 
                if (projectFilesDict.TryGetValue(registerdFile.DataRelPath, out var srcProjectFile))
                {
                    if (srcProjectFile.DataHash != registerdFile.DataHash)
                    {
                        ProjectFile srcFile = new ProjectFile(srcProjectFile, DataState.None, registerdFile.DataSrcPath);
                        ProjectFile dstFile = new ProjectFile(registerdFile, DataState.Modified, srcProjectFile.DataSrcPath);
                        ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataState.Modified, true);
                        registeredChangesDict.TryAdd(registerdFile.DataRelPath, newChange);
                    }
                    else
                        continue;
                }
                else
                {
                    ProjectFile srcFile = new ProjectFile(registerdFile, DataState.None);
                    ProjectFile dstFile = new ProjectFile(registerdFile, DataState.Added, CurrentProjectData.ProjectPath);
                    registeredChangesDict.TryAdd(registerdFile.DataRelPath, new ChangedFile(srcFile, dstFile, DataState.Added));
                }
            }
            preStagedFilesDict.Clear();
            DataStagedEventHandler?.Invoke(registeredChangesDict.Values.ToList());
        }

        /// <summary>
        /// Clears All the prestagedFiles, Clears StagedFiles Except those registered as IntegrityChecked
        /// </summary>
        public void ClearDeployedFileChanges()
        {
            preStagedFilesDict.Clear();
            List<ChangedFile> clearChangedList = new List<ChangedFile>();
            foreach (ChangedFile changedFile in registeredChangesDict.Values)
            {
                if ((changedFile.DataState & DataState.IntegrityChecked) == 0)
                {
                    clearChangedList.Add(changedFile);
                }
            }
            foreach (ChangedFile idenfitiedChange in clearChangedList)
            {
                if (idenfitiedChange.DstFile != null)
                    registeredChangesDict.Remove(idenfitiedChange.DstFile.DataRelPath);
            }
            DataStagedEventHandler?.Invoke(registeredChangesDict.Values.ToList());
        }
        #endregion
        #region CallBacks From Parent Model 
        public void ProjectLoadedCallback(object projObj)
        {
            if (projObj is not ProjectData loadedProject) return;

            preStagedFilesDict.Clear();
            registeredChangesDict.Clear();
            this.currentProjectData = loadedProject;
            this.projectFilesDict = currentProjectData.ProjectFiles;
            DataStagedEventHandler?.Invoke(registeredChangesDict.Values.ToList());
        }
        public void MetaDataLoadedCallBack(object metaDataObj)
        {
            if (metaDataObj is not ProjectMetaData projectMetaData) return;
            if (projectMetaData == null) return;
            this.backupFiles = projectMetaData.BackupFiles;
        }
        #endregion
    }
}
#region Deprecated 
//public void StageNewFiles(ProjectData srcProjectData)
//{
//    if (currentProjectData == null)
//    {
//        MessageBox.Show("Project Main is Missing for FileManager");
//        return;
//    }
//    List<ChangedFile>? changedFiles = FindVersionDifferences(srcProjectData, currentProjectData, false);
//    UpdateStageFileList(changedFiles);
//}
//private void UpdateStageFileList(List<ChangedFile> changedFiles)
//{
//    if (preStagedFilesDict.Count <= 0) return;
//    foreach (ChangedFile changes in changedFiles)
//    {
//        if (changes.SrcFile == null) return;
//        changes.SrcFile.DataState &= ~DataState.PreStaged;

//        if (changes.DstFile == null) return;
//        changes.DstFile.DataState &= ~DataState.PreStaged;
//    }
//    foreach (ProjectFile registeredFile in projectFilesDict.Values)
//    {
//        registeredFile.DataState &= ~DataState.PreStaged;
//        if ((registeredFile.DataState & DataState.Deleted) != 0)
//        {
//            ChangedFile newChange = new ChangedFile(new ProjectFile(registeredFile), DataState.Deleted);
//            registeredChangesDict.Add(registeredFile.DataRelPath, newChange);
//        }
//        //compare the hash value, and if its the same, request to remove that file. 
//        if (projectFilesDict.TryGetValue(registeredFile.DataRelPath, out var srcProjectFile))
//        {
//            if (srcProjectFile.DataHash != registeredFile.DataHash)
//            {
//                ProjectFile srcFile = new ProjectFile(srcProjectFile);
//                ProjectFile dstFile = new ProjectFile(registeredFile, DataState.Modified);
//                ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataState.Modified);
//                registeredChangesDict.Add(registeredFile.DataRelPath, newChange);
//            }
//            else continue;
//        }
//        else
//        {
//            ProjectFile newFile = new ProjectFile(registeredFile, DataState.Added);
//            ChangedFile newChange = new ChangedFile(newFile, DataState.Added);
//            registeredChangesDict.Add(registeredFile.DataRelPath, newChange);
//        }
//    }
//    DataStagedEventHandler?.Invoke(registeredChangesDict.Values.ToList());
//    DataPreStagedEventHandler?.Invoke(preStagedFilesDict.Values.ToList());
//}
#endregion