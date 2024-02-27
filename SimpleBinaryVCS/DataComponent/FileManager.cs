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
        Backup = 1 << 6, 
        Overlapped = 1 << 7
    }
    public class FileManager : IManager
    {
        #region Class Variables 
        private Dictionary<string, ProjectFile> _backupFiles;
        private Dictionary<string, ProjectFile> _projectFilesDict;
        private Dictionary<string, ProjectFile> _preStagedFilesDict;
        private Dictionary<string, ChangedFile> _registeredChangesDict; 
        private SemaphoreSlim _asyncControl;
        private FileHandlerTool _fileHandlerTool;
        private ProjectData? _dstProjectData;
        private ProjectData? _srcProjectData;
        private bool _hasIntegrityIssue;
        #endregion

        #region Manager Events 
        public event Action<ProjectData>? SrcProjectDataLoadedEventHandler;
        public event Action<List<ChangedFile>>? OverlappedFileFoundEventHandler;
        public event Action<object>? DataStagedEventHandler;
        public event Action<object>? DataPreStagedEventHandler;
        public event Action<object>? PreStagedDataOverlapEventHandler;
        public event Action<object, string, List<ProjectFile>>? IntegrityCheckEventHandler;
        public event Action<string> IssueEventHandler;
        #endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public FileManager()
        {
            _projectFilesDict = new Dictionary<string, ProjectFile>();
            _preStagedFilesDict = new Dictionary<string, ProjectFile>();
            _registeredChangesDict = new Dictionary<string, ChangedFile>();
            _fileHandlerTool = new FileHandlerTool();
            _asyncControl = new SemaphoreSlim(8);
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public void Awake()
        {
        }
        #region Calls For File Differences
        public async void MainProjectIntegrityCheck(object sender)
        {
            ProjectData? mainProject = _dstProjectData;
            if (mainProject == null)
            {
                MessageBox.Show("Main Project is Missing");
                return;
            }
            _preStagedFilesDict.Clear();
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

                string[]? rawFiles = await Task.Run( () => Directory.GetFiles(mainProject.ProjectPath, "*", SearchOption.AllDirectories));
                foreach (string absPathFile in rawFiles)
                {
                    directoryFiles.Add(Path.GetRelativePath(mainProject.ProjectPath, absPathFile));
                }

                string[]? rawDirs = await Task.Run( () => Directory.GetDirectories(mainProject.ProjectPath, "*", SearchOption.AllDirectories));
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
                    _registeredChangesDict.Add(dstFile.DataRelPath, newChange);
                    _preStagedFilesDict.Add(dirRelPath, dstFile);
                }

                foreach (string dirRelPath in deletedDirs)
                {
                    ProjectFile dstFile = new ProjectFile(mainProject.ProjectPath, dirRelPath, null, DataState.Deleted | DataState.IntegrityChecked, ProjectDataType.Directory);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Deleted | DataState.IntegrityChecked);
                    _preStagedFilesDict.Add(dirRelPath, dstFile);
                    _registeredChangesDict.Add(dstFile.DataRelPath, newChange);
                }

                foreach (string fileRelPath in addedFiles)
                {
                    if (fileRelPath == "ProjectMetaData.bin") continue;
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(mainProject.ProjectPath, fileRelPath);
                    ProjectFile dstFile = new ProjectFile(mainProject.ProjectPath, fileRelPath, fileHash, DataState.Added | DataState.IntegrityChecked, ProjectDataType.Directory);
                    _preStagedFilesDict.Add(fileRelPath, dstFile);
                    _registeredChangesDict.Add(dstFile.DataRelPath, new ChangedFile(dstFile, DataState.Added | DataState.IntegrityChecked));
                }

                foreach (string fileRelPath in deletedFiles)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile srcFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.None);
                    ProjectFile dstFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.Deleted | DataState.IntegrityChecked);
                    dstFile.UpdatedTime = DateTime.Now;
                    _preStagedFilesDict.Add(fileRelPath, dstFile);
                    _registeredChangesDict.Add(dstFile.DataRelPath, new ChangedFile(srcFile, dstFile, DataState.Deleted | DataState.IntegrityChecked, true));
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
                        _preStagedFilesDict.Add(fileRelPath, dstFile);
                        _registeredChangesDict.Add(dstFile.DataRelPath, new ChangedFile(srcFile, dstFile, DataState.Modified | DataState.IntegrityChecked, true));
                    }
                }
                fileIntegrityLog.AppendLine("Integrity Check Complete");
                DataStagedEventHandler?.Invoke(_registeredChangesDict.Values.ToList());
                IntegrityCheckEventHandler?.Invoke(sender, fileIntegrityLog.ToString(), _preStagedFilesDict.Values.ToList());
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
                    if (!_backupFiles.TryGetValue(srcDict[fileRelPath].DataHash, out ProjectFile? backupFile))
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
                        if (!_backupFiles.TryGetValue(srcDict[fileRelPath].DataHash, out ProjectFile? backupFile))
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

                Dictionary<string, ProjectFile> srcDict = _preStagedFilesDict;
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

        #region Calls For File PreStage Update
        public async void RetrieveDataSrc(string srcPath)
        {
            try
            {
                string[] binFiles = Directory.GetFiles(srcPath, "VersionLog.*", SearchOption.AllDirectories);
                if (binFiles.Length == 1)
                {
                    var stream = File.ReadAllBytes(binFiles[0]);
                    bool result = _fileHandlerTool.TryDeserializeProjectData(binFiles[0], out ProjectData? srcProjectData);
                    if (srcProjectData != null)
                    {
                        srcProjectData.ProjectPath = srcPath;
                        srcProjectData.SetProjectFilesSrcPath();
                        RegisterNewData(srcProjectData);
                        _srcProjectData = srcProjectData;
                        SrcProjectDataLoadedEventHandler?.Invoke(srcProjectData);
                    }
                }
                else
                {
                    await RegisterNewData(srcPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File Manager RetrieveDataSrc Error: {ex.Message}");
            }
        }
        public async Task RegisterNewData(string updateDirPath)
        {
            string[]? filesFullPaths;
            string[]? dirsFullPaths;
            try
            {
                filesFullPaths = await Task.Run(() => Directory.GetFiles(updateDirPath, "*", SearchOption.AllDirectories));
                dirsFullPaths = await Task.Run(() => Directory.GetDirectories(updateDirPath, "*", SearchOption.AllDirectories));
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
                    if (!_preStagedFilesDict.TryAdd(newFile.DataRelPath, newFile))
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
                    if (!_preStagedFilesDict.TryAdd(newFile.DataRelPath, newFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {newFile.DataName}: for Update");
                    }
                    else continue;
                }
                DataPreStagedEventHandler?.Invoke(_preStagedFilesDict.Values.ToList());
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
                    if (!_preStagedFilesDict.TryAdd(srcFile.DataRelPath, srcFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {srcFile.DataName}: for Update");
                    }
                    else continue;
                }
                DataPreStagedEventHandler?.Invoke(_preStagedFilesDict.Values.ToList());
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show(ex.Message);
                return;
            }
        }
        public void RegisterNewfile(ProjectFile projectFile, DataState fileState)
        {
            ProjectFile newfile = new ProjectFile(projectFile, fileState | DataState.PreStaged);
            if (!_preStagedFilesDict.TryAdd(newfile.DataRelPath, newfile))
            {
                PreStagedDataOverlapEventHandler?.Invoke(newfile);
                return;
            }
            DataPreStagedEventHandler?.Invoke(_preStagedFilesDict.Values.ToList());
        }

        #endregion

        #region Calls for File Stage Update
        public async void StageNewFilesAsync()
        {
            if (_dstProjectData == null)
            {
                MessageBox.Show("Project Data is unreachable from FileManager");
                return;
            }
            await HashPreStagedFilesAsync();
            UpdateStageFileList();
        }
        private async Task HashPreStagedFilesAsync()
        {
            try
            {
                if (_preStagedFilesDict.Count <= 0) return;
                List<Task> asyncTasks = new List<Task>();
                //Update changedFilesDict
                foreach (ProjectFile file in _preStagedFilesDict.Values)
                {
                    if (file.DataType == ProjectDataType.Directory || file.DataHash != "") continue;
                    asyncTasks.Add(Task.Run(async () =>
                    {
                        await _asyncControl.WaitAsync();
                        try
                        {
                            await HashTool.GetFileMD5CheckSumAsync(file);
                        }
                        finally
                        {
                            _asyncControl.Release();
                        }
                    }));
                }
                await Task.WhenAll(asyncTasks);
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"File Manager UpdateHashFromChangedList Error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine(_asyncControl.CurrentCount);
            }
        }
        private void CheckFileOverlap()
        {

        }
        private void UpdateStageFileList()
        {
            if (_preStagedFilesDict.Count <= 0) return;
            foreach (ProjectFile registerdFile in _preStagedFilesDict.Values)
            {
                
                registerdFile.DataState &= ~DataState.PreStaged;
                if ((registerdFile.DataState & DataState.Restored) != 0 && registerdFile.DataType != ProjectDataType.Directory)
                {
                    _backupFiles.TryGetValue(registerdFile.DataHash, out ProjectFile? backupFile);
                    if (backupFile != null)
                    {
                        //File Modified 
                        if (_projectFilesDict.TryGetValue(backupFile.DataRelPath, out ProjectFile? projectFile))
                        {
                            ProjectFile srcFile = new ProjectFile(projectFile, DataState.Backup, backupFile.DataSrcPath);
                            ProjectFile dstFile = new ProjectFile(registerdFile, DataState.Modified);
                            ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataState.Modified, true);
                            _registeredChangesDict.TryAdd(registerdFile.DataRelPath, newChange);
                        }
                        else
                        {
                            ProjectFile srcFile = new ProjectFile(registerdFile, DataState.Backup, backupFile.DataSrcPath);
                            ProjectFile dstFile = new ProjectFile(registerdFile, DataState.Restored, _dstProjectData.ProjectPath);
                            ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataState.Restored, true);
                            _registeredChangesDict.TryAdd(registerdFile.DataRelPath, newChange);
                        }
                    }
                    //Reset SrcPath to BackupPath
                }
                if ((registerdFile.DataState & DataState.Deleted) != 0)
                {
                    ChangedFile newChange = new ChangedFile(new ProjectFile(registerdFile), DataState.Deleted);
                    _registeredChangesDict.TryAdd(registerdFile.DataRelPath, newChange);
                }
                //compare the hash value, and if its the same, request to remove that file. 
                if (_projectFilesDict.TryGetValue(registerdFile.DataRelPath, out var srcProjectFile))
                {
                    if (srcProjectFile.DataHash != registerdFile.DataHash)
                    {
                        ProjectFile srcFile = new ProjectFile(srcProjectFile, DataState.None, registerdFile.DataSrcPath);
                        ProjectFile dstFile = new ProjectFile(registerdFile, DataState.Modified, srcProjectFile.DataSrcPath);
                        ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataState.Modified, true);
                        _registeredChangesDict.TryAdd(registerdFile.DataRelPath, newChange);
                    }
                    else
                        continue;
                }
                else
                {
                    ProjectFile srcFile = new ProjectFile(registerdFile, DataState.None);
                    ProjectFile dstFile = new ProjectFile(registerdFile, DataState.Added, _dstProjectData.ProjectPath);
                    _registeredChangesDict.TryAdd(registerdFile.DataRelPath, new ChangedFile(srcFile, dstFile, DataState.Added));
                }
            }
            _preStagedFilesDict.Clear();
            DataStagedEventHandler?.Invoke(_registeredChangesDict.Values.ToList());
        }

        public async void StageNewFilesAsync(string deployPath)
        {
            if (_dstProjectData == null)
            {
                MessageBox.Show("Project Data is unreachable from FileManager");
                return;
            }
            await HashPreStagedFilesAsync();
            UpdateStageFileList();
            (bool result, List<string>? failedDataList) = DeployFileIntegrityCheck(deployPath);
            if (!result)
            {
                MessageBox.Show("Failed to Stage Changes: DeployFiles Integrity Check Failed"); 
                return;
            }
        }

        private (bool, List<string>? failedFileList) DeployFileIntegrityCheck(string deployPath)
        {
            try
            {
                List<string> failedList = new List<string>();
                foreach(ChangedFile changes in _registeredChangesDict.Values)
                {
                    if (changes.SrcFile == null && changes.DstFile != null) failedList.Add(changes.DstFile.DataName); 
                    
                }
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"{ex.Message}, Failed SrcFileIntegrityCheck On FileManager");
            }
        }
        /// <summary>
        /// Clears All the prestagedFiles, Clears StagedFiles Except those registered as IntegrityChecked
        /// </summary>
        public void ClearDeployedFileChanges()
        {
            _preStagedFilesDict.Clear();
            List<ChangedFile> clearChangedList = new List<ChangedFile>();
            foreach (ChangedFile changedFile in _registeredChangesDict.Values)
            {
                if ((changedFile.DataState & DataState.IntegrityChecked) == 0)
                {
                    clearChangedList.Add(changedFile);
                }
            }
            foreach (ChangedFile idenfitiedChange in clearChangedList)
            {
                if (idenfitiedChange.DstFile != null)
                    _registeredChangesDict.Remove(idenfitiedChange.DstFile.DataRelPath);
            }
            DataStagedEventHandler?.Invoke(_registeredChangesDict.Values.ToList());
        }
        #endregion

        #region CallBacks From Parent Model 
        public void ProjectLoadedCallback(object projObj)
        {
            if (projObj is not ProjectData loadedProject) return;

            _preStagedFilesDict.Clear();
            _registeredChangesDict.Clear();
            this._dstProjectData = loadedProject;
            this._projectFilesDict = _dstProjectData.ProjectFiles;
            DataStagedEventHandler?.Invoke(_registeredChangesDict.Values.ToList());
        }
        public void MetaDataLoadedCallBack(object metaDataObj)
        {
            if (metaDataObj is not ProjectMetaData projectMetaData) return;
            if (projectMetaData == null) return;
            this._backupFiles = projectMetaData.BackupFiles;
        }
        #endregion
    }
}