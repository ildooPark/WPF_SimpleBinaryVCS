using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.Generic;
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
        private Dictionary<string, ProjectFile> _backupFilesDict;
        /// <summary>
        /// Imported ProjectMainFilesDict
        /// </summary>
        private Dictionary<string, ProjectFile> _projectFilesDict;
        private Dictionary<ProjectFile, List<ProjectFile>> _projectFilesDict_namesSorted;
        /// <summary>
        /// Key: DataRelPath Value: ProjectFile
        /// </summary>
        private Dictionary<string, ProjectFile> _preStagedFilesDict;
        /// <summary>
        /// Key: DstFile DataRelPath Value: ChangedFile
        /// </summary>
        private Dictionary<string, ChangedFile> _registeredChangesDict; 
        private SemaphoreSlim _asyncControl;
        private FileHandlerTool _fileHandlerTool;
        private HashTool _hashTool; 
        private ProjectData? _dstProjectData;
        private ProjectData? _srcProjectData;
        #endregion

        #region Manager Events 
        public event Action<ProjectData>? SrcProjectDataLoadedEventHandler;
        public event Action<List<ChangedFile>, List<ChangedFile>>? OverlappedFileFoundEventHandler;
        public event Action<object>? DataStagedEventHandler;
        public event Action<object>? DataPreStagedEventHandler;
        public event Action<object>? PreStagedDataOverlapEventHandler;
        public event Action<string, List<ProjectFile>>? IntegrityCheckEventHandler;
        public event Action<MetaDataState> IssueEventHandler;
        #endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public FileManager()
        {
            _projectFilesDict = new Dictionary<string, ProjectFile>();
            _projectFilesDict_namesSorted = new Dictionary<ProjectFile, List<ProjectFile>>();
            _preStagedFilesDict = new Dictionary<string, ProjectFile>();
            _registeredChangesDict = new Dictionary<string, ChangedFile>();
            _fileHandlerTool = App.FileHandlerTool;
            _hashTool = App.HashTool;
            _asyncControl = new SemaphoreSlim(8);
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public void Awake()
        {
        }
        #region Calls For File Differences
        public async void MainProjectIntegrityCheck()
        {
            ProjectData? mainProject = _dstProjectData;
            IssueEventHandler?.Invoke(MetaDataState.IntegrityChecking);
            if (mainProject == null)
            {
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                MessageBox.Show("Main Project is Missing");
                return;
            }
            _preStagedFilesDict.Clear();
            try
            {
                Stopwatch sw = new Stopwatch(); 
                sw.Start();
                StringBuilder fileIntegrityLog = new StringBuilder();
                List<ChangedFile> fileChanges = new List<ChangedFile>();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {mainProject.UpdatedVersion}");

                Dictionary<string, ProjectFile> projectFilesDict = mainProject.ProjectFiles;
                string backupPath = $"{mainProject.ProjectPath}\\Backup_{mainProject.ProjectName}";
                string exportPath = $"{mainProject.ProjectPath}\\Export_{mainProject.ProjectName}";
                List<string> recordedFiles = mainProject.ProjectRelFilePathsList;
                List<string> recordedDirs = mainProject.ProjectRelDirsList;

                List<string> directoryRelFiles = new List<string>();
                List<string> directoryRelDirs = new List<string>();

                if (!Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);
                if (!Directory.Exists(exportPath)) Directory.CreateDirectory(exportPath);
                var backupFilesTask = Task.Run(() => Directory.GetFiles(backupPath, "*", SearchOption.AllDirectories));
                var backupDirsTask = Task.Run(() => Directory.GetDirectories(backupPath, "*", SearchOption.AllDirectories));
                var exportFilesTask = Task.Run(() => Directory.GetFiles(exportPath, "*", SearchOption.AllDirectories));
                var exportDirsTask = Task.Run(() => Directory.GetDirectories(exportPath, "*", SearchOption.AllDirectories));
                var rawFilesTask = Task.Run(() => Directory.GetFiles(mainProject.ProjectPath, "*", SearchOption.AllDirectories));
                var rawDirsTask = Task.Run(() => Directory.GetDirectories(mainProject.ProjectPath, "*", SearchOption.AllDirectories));


                string[]? backupFiles = await backupFilesTask;
                string[]? backupDirs = await backupDirsTask;
                string[]? exportFiles = await exportFilesTask;
                string[]? exportDirs = await exportDirsTask;
                string[]? rawFiles = await rawFilesTask;
                string[]? rawDirs = await rawDirsTask;
                if (backupFiles == null) backupFiles = new string[0];
                if (backupDirs == null) backupDirs = new string[0];
                if (exportFiles == null) exportFiles = new string[0];
                if (exportDirs == null) exportDirs = new string[0];
                if (rawFiles == null) backupFiles = new string[0];
                if (rawDirs == null) backupDirs = new string[0];

                IEnumerable<string> directoryFiles = rawFiles.ToList().Except(backupFiles.ToList()); directoryFiles = directoryFiles.Except(exportFiles.ToList());
                IEnumerable<string> directoryDirs = rawDirs.ToList().Except(backupDirs.ToList()); directoryDirs = directoryDirs.Except(exportDirs.ToList());
                fileIntegrityLog.Append(sw.Elapsed.ToString());

                foreach (string absPathFile in directoryFiles)
                {
                    directoryRelFiles.Add(Path.GetRelativePath(mainProject.ProjectPath, absPathFile));
                }

                foreach (string absPathDir in directoryDirs)
                {
                    directoryRelDirs.Add(Path.GetRelativePath(mainProject.ProjectPath, absPathDir));
                }

                IEnumerable<string> addedFiles = directoryRelFiles.Except(recordedFiles);
                IEnumerable<string> addedDirs = directoryRelDirs.Except(recordedDirs);
                IEnumerable<string> deletedFiles = recordedFiles.Except(directoryRelFiles);
                IEnumerable<string> deletedDirs = recordedDirs.Except(directoryRelDirs);
                IEnumerable<string> intersectFiles = recordedFiles.Intersect(directoryRelFiles);

                foreach (string dirRelPath in addedDirs)
                {
                    if (dirRelPath == $"Backup_{mainProject.ProjectName}" || dirRelPath == $"Export_{ mainProject.ProjectName}") continue;
                    ProjectFile dstFile = new ProjectFile(mainProject.ProjectPath, dirRelPath, null, DataState.Added | DataState.IntegrityChecked, ProjectDataType.Directory);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Added | DataState.IntegrityChecked);
                    _registeredChangesDict.TryAdd(dstFile.DataRelPath, newChange);
                    _preStagedFilesDict.TryAdd(dirRelPath, dstFile);
                }

                foreach (string dirRelPath in deletedDirs)
                {
                    ProjectFile dstFile = new ProjectFile(mainProject.ProjectPath, dirRelPath, null, DataState.Deleted | DataState.IntegrityChecked, ProjectDataType.Directory);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Deleted | DataState.IntegrityChecked);
                    _preStagedFilesDict.TryAdd(dirRelPath, dstFile);
                    _registeredChangesDict.TryAdd(dstFile.DataRelPath, newChange);
                }

                foreach (string fileRelPath in addedFiles)
                {
                    if (fileRelPath == "ProjectMetaData.bin") continue;
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = _hashTool.GetFileMD5CheckSum(mainProject.ProjectPath, fileRelPath);
                    ProjectFile dstFile = new ProjectFile(mainProject.ProjectPath, fileRelPath, fileHash, DataState.Added | DataState.IntegrityChecked, ProjectDataType.File);
                    _preStagedFilesDict.TryAdd(fileRelPath, dstFile);
                    _registeredChangesDict.TryAdd(dstFile.DataRelPath, new ChangedFile(dstFile, DataState.Added | DataState.IntegrityChecked));
                }

                foreach (string fileRelPath in deletedFiles)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile srcFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.None);
                    ProjectFile dstFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.Deleted | DataState.IntegrityChecked);
                    dstFile.UpdatedTime = DateTime.Now;
                    _preStagedFilesDict.TryAdd(fileRelPath, dstFile);
                    _registeredChangesDict.TryAdd(dstFile.DataRelPath, new ChangedFile(srcFile, dstFile, DataState.Deleted | DataState.IntegrityChecked, true));
                }
                Dictionary<string, ProjectFile> intersectedFiles = new Dictionary<string, ProjectFile> ();
                List<Task> asyncHashing = new List<Task> ();
                foreach (string fileRelPath in intersectFiles)
                {
                    if (projectFilesDict[fileRelPath].DataType == ProjectDataType.Directory) continue;
                    if (!intersectedFiles.TryAdd(fileRelPath, new ProjectFile(projectFilesDict[fileRelPath])))
                    {
                        IssueEventHandler?.Invoke(MetaDataState.Idle);
                        System.Windows.MessageBox.Show($"Couldn't Run File Integrity Check, Couldn't Hash Intersected File on {fileRelPath}");
                        return;
                    }
                    asyncHashing.Add(Task.Run(async () =>
                    {
                        await _asyncControl.WaitAsync(); 
                        try
                        {
                            _hashTool.GetFileMD5CheckSum(mainProject.ProjectPath, fileRelPath);
                        }
                        catch (Exception ex)
                        {
                            IssueEventHandler?.Invoke(MetaDataState.Idle);
                            MessageBox.Show($"Couldn't Run File Integrity Check: File async Hashing Failed\n{ex.Message}");
                            return;
                        }
                        finally
                        {
                            _asyncControl.Release();
                        }
                    }));
                }
                await Task.WhenAll(asyncHashing);
                foreach (ProjectFile intersectedFile in intersectedFiles.Values)
                {
                    if (!projectFilesDict.TryGetValue(intersectedFile.DataRelPath, out ProjectFile? projectFile))
                    {
                        IssueEventHandler?.Invoke(MetaDataState.Idle);
                        System.Windows.MessageBox.Show($"Couldn't Run File Integrity Check, project File does not exist in Intersected file list {intersectedFile.DataName}");
                        return;
                    }
                    if (projectFile.DataHash != intersectedFile.DataHash)
                    {
                        fileIntegrityLog.AppendLine($"File {projectFile.DataName} on {projectFile.DataRelPath} has been modified");

                        ProjectFile srcFile = new ProjectFile(projectFile, DataState.None);
                        ProjectFile dstFile = new ProjectFile(projectFile, DataState.Modified | DataState.IntegrityChecked);
                        dstFile.BuildVersion = FileVersionInfo.GetVersionInfo(Path.Combine(mainProject.ProjectPath, projectFile.DataRelPath)).FileVersion ?? "";
                        dstFile.DataSize = new FileInfo(Path.Combine(mainProject.ProjectPath, projectFile.DataRelPath)).Length;
                        dstFile.DataHash = intersectedFile.DataHash;
                        dstFile.UpdatedTime = new FileInfo(srcFile.DataAbsPath).LastAccessTime;
                        _preStagedFilesDict.TryAdd(projectFile.DataRelPath, dstFile);
                        _registeredChangesDict.TryAdd(dstFile.DataRelPath, new ChangedFile(srcFile, dstFile, DataState.Modified | DataState.IntegrityChecked, true));
                    }
                }
                sw.Stop();
                fileIntegrityLog.Append($"Integrity Check Took: {sw.ToString()}s");
                fileIntegrityLog.AppendLine("Integrity Check Complete");
                DataStagedEventHandler?.Invoke(_registeredChangesDict.Values.ToList());
                IntegrityCheckEventHandler?.Invoke(fileIntegrityLog.ToString(), _preStagedFilesDict.Values.ToList());
                IssueEventHandler?.Invoke(MetaDataState.Idle);
            }

            catch (Exception ex)
            {
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run File Integrity Check");
            }
        }
        public List<ChangedFile>? ProjectIntegrityCheck(ProjectData targetProject)
        {
            if (targetProject == null)
            {
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                MessageBox.Show("Main Project is Missing");
                return null;
            }
            IssueEventHandler?.Invoke(MetaDataState.CleanRestoring);
            _preStagedFilesDict.Clear();
            _registeredChangesDict.Clear();
            try
            {
                List<ChangedFile> fileChanges = new List<ChangedFile>();

                Dictionary<string, ProjectFile> projectFilesDict = targetProject.ProjectFiles;
                string backupPath = $"{targetProject.ProjectPath}\\Backup_{targetProject.ProjectName}";
                string exportPath = $"{targetProject.ProjectPath}\\Export_{targetProject.ProjectName}";
                List<string> recordedFiles = targetProject.ProjectRelFilePathsList;
                List<string> recordedDirs = targetProject.ProjectRelDirsList;

                List<string> directoryRelFiles = new List<string>();
                List<string> directoryRelDirs = new List<string>();

                if (!Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);
                if (!Directory.Exists(exportPath)) Directory.CreateDirectory(exportPath);
                string[]? backupFiles = Directory.GetFiles(backupPath, "*", SearchOption.AllDirectories);
                string[]? backupDirs = Directory.GetDirectories(backupPath, "*", SearchOption.AllDirectories);
                if (backupFiles == null) backupFiles = new string[0];
                if (backupDirs == null) backupDirs = new string[0];
                string[]? exportFiles = Directory.GetFiles(exportPath, "*", SearchOption.AllDirectories);
                string[]? exportDirs = Directory.GetDirectories(exportPath, "*", SearchOption.AllDirectories);
                if (exportFiles == null) exportFiles = new string[0];
                if (exportDirs == null) exportDirs = new string[0];

                string[]? rawFiles = Directory.GetFiles(targetProject.ProjectPath, "*", SearchOption.AllDirectories);
                string[]? rawDirs = Directory.GetDirectories(targetProject.ProjectPath, "*", SearchOption.AllDirectories);
                if (rawFiles == null) backupFiles = new string[0];
                if (rawDirs == null) backupDirs = new string[0];

                IEnumerable<string> directoryFiles = rawFiles.ToList().Except(backupFiles.ToList()); directoryFiles = directoryFiles.Except(exportFiles.ToList());
                IEnumerable<string> directoryDirs = rawDirs.ToList().Except(backupDirs.ToList()); directoryDirs = directoryDirs.Except(exportDirs.ToList());

                foreach (string absPathFile in directoryFiles)
                {
                    directoryRelFiles.Add(Path.GetRelativePath(targetProject.ProjectPath, absPathFile));
                }

                foreach (string absPathDir in directoryDirs)
                {
                    directoryRelDirs.Add(Path.GetRelativePath(targetProject.ProjectPath, absPathDir));
                }

                IEnumerable<string> filesToDelete = directoryRelFiles.Except(recordedFiles);
                IEnumerable<string> dirsToDelete = directoryRelDirs.Except(recordedDirs);
                IEnumerable<string> filesToAdd = recordedFiles.Except(directoryRelFiles);
                IEnumerable<string> dirsToAdd = recordedDirs.Except(directoryRelDirs);
                IEnumerable<string> intersectFiles = recordedFiles.Intersect(directoryRelFiles);

                foreach (string dirRelPath in dirsToDelete)
                {
                    if (dirRelPath == $"Backup_{targetProject.ProjectName}" || dirRelPath == $"Export_{targetProject.ProjectName}") continue;
                    ProjectFile dstFile = new ProjectFile(targetProject.ProjectPath, dirRelPath, null, DataState.Deleted, ProjectDataType.Directory);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Deleted);
                    fileChanges.Add(newChange);
                }

                foreach (string dirRelPath in dirsToAdd)
                {
                    ProjectFile dstFile = new ProjectFile(targetProject.ProjectPath, dirRelPath, null, DataState.Added, ProjectDataType.Directory);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Added);
                    fileChanges.Add(newChange);
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    if (fileRelPath == "ProjectMetaData.bin") continue;
                    ProjectFile dstFile = new ProjectFile(targetProject.ProjectPath, fileRelPath, null, DataState.Deleted, ProjectDataType.File);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Deleted);
                    fileChanges.Add(newChange);
                }

                foreach (string fileRelPath in filesToAdd)
                {
                    if (!_backupFilesDict.TryGetValue(projectFilesDict[fileRelPath].DataHash, out ProjectFile? backupFile))
                    {
                        MessageBox.Show($"Failed To Retrieve File {projectFilesDict[fileRelPath].DataName} For Restoration");
                        return null;
                    }
                    ProjectFile srcFile = new ProjectFile(backupFile, DataState.None);
                    ProjectFile dstFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.Added);
                    ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataState.Added, true);
                    fileChanges.Add(newChange);
                }

                foreach (string fileRelPath in intersectFiles)
                {
                    string? dirFileHash = _hashTool.GetFileMD5CheckSum(targetProject.ProjectPath, fileRelPath);
                    if (projectFilesDict[fileRelPath].DataHash != dirFileHash)
                    {
                        if (!_backupFilesDict.TryGetValue(projectFilesDict[fileRelPath].DataHash, out ProjectFile? backupFile))
                        {
                            MessageBox.Show($"Failed To Retrieve File {projectFilesDict[fileRelPath].DataName} For Restoration");
                            return null; 
                        }
                        ProjectFile srcFile = new ProjectFile(backupFile, DataState.None);
                        ProjectFile dstFile = new ProjectFile(backupFile, DataState.Restored, targetProject.ProjectPath);
                        dstFile.DataRelPath = fileRelPath;
                        ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataState.Modified, true);
                        fileChanges.Add(newChange);
                    }
                }

                IssueEventHandler?.Invoke(MetaDataState.Idle);
                return fileChanges;
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run Version Clearn Restoring File Check");
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                return null;
            }
        }
        /// <summary>
        /// Merging Src => Dst, Occurs in version Reversion or Merging from outer source. 
        /// </summary>
        /// <param name="isRevert"> True if Reverting, else (Merge) false.</param>
        public List<ChangedFile>? FindVersionDifferences(ProjectData srcData, ProjectData dstData, bool isProjectRevert)
        {
            if (!isProjectRevert)
                return FindVersionDifferences(srcData, dstData);
            IssueEventHandler?.Invoke(MetaDataState.Processing);
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
                    ProjectFile dstDir = new ProjectFile(srcDict[dirRelPath], DataState.Added, dstData.ProjectPath);
                    ChangedFile newChange = new ChangedFile(dstDir, DataState.Added);
                    fileChanges.Add(newChange);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    ProjectFile dstDir = new ProjectFile(dstDict[dirRelPath], DataState.Deleted);
                    fileChanges.Add(new ChangedFile(dstDir, DataState.Deleted));
                }

                foreach (string fileRelPath in filesToAdd)
                {
                    if (!_backupFilesDict.TryGetValue(srcDict[fileRelPath].DataHash, out ProjectFile? backupFile))
                    {
                        MessageBox.Show($"Following Previous Project Version {srcData.UpdatedVersion}\n" +
                            $"Lacks Backup File {srcDict[fileRelPath].DataName}");
                        return null;
                    }
                    ProjectFile srcFile = new ProjectFile(backupFile, DataState.Backup, backupFile.DataSrcPath);
                    srcDict.TryGetValue(fileRelPath, out ProjectFile? recordedSrcFile);
                    ProjectFile dstFile = new ProjectFile(recordedSrcFile, DataState.Restored, dstData.ProjectPath);
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
                        if (!_backupFilesDict.TryGetValue(srcDict[fileRelPath].DataHash, out ProjectFile? backupFile))
                        {
                            MessageBox.Show($"Following Previous Project Version {srcData.UpdatedVersion} Lacks Backup File {srcDict[fileRelPath].DataName}");
                            return null;
                        }
                        ProjectFile srcFile = new ProjectFile(backupFile, DataState.Backup, backupFile.DataSrcPath);
                        srcFile.DataHash = dstDict[fileRelPath].DataHash;
                        ProjectFile dstFile = new ProjectFile(backupFile, DataState.Restored, dstDict[fileRelPath].DataSrcPath);
                        dstFile.DataRelPath = fileRelPath;
                        fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Restored, true));
                    }
                }
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                DataStagedEventHandler?.Invoke(fileChanges);
                return fileChanges;
            }
            catch (Exception ex)
            {
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run Find Version Differences For Backup");
                return null;
            }
        }
        public List<ChangedFile>? FindVersionDifferences(ProjectData srcData, ProjectData dstData)
        {
            try
            {
                IssueEventHandler?.Invoke(MetaDataState.Processing);
                List<ChangedFile> fileChanges = new List<ChangedFile>();

                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();

                Dictionary<string, ProjectFile> srcDict = srcData.ProjectFiles;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFiles;

                // Files which is not on the Dst 
                IEnumerable<string> filesOnSrc = srcData.ProjectRelFilePathsList.Except(dstData.ProjectRelFilePathsList);
                // Files which is not on the Src
                IEnumerable<string> filesOnDst = dstData.ProjectRelFilePathsList.Except(srcData.ProjectRelFilePathsList);
                // Directories which is not on the Src
                IEnumerable<string> dirsOnSrc = srcData.ProjectRelDirsList.Except(dstData.ProjectRelDirsList);
                // Directories which is not on the Dst
                IEnumerable<string> dirsOnDst = dstData.ProjectRelDirsList.Except(srcData.ProjectRelDirsList);
                // Files to Overwrite
                IEnumerable<string> intersectFiles = srcData.ProjectRelFilePathsList.Intersect(dstData.ProjectRelFilePathsList);

                foreach (string dirRelPath in dirsOnSrc)
                {
                    ProjectFile srcDir = new ProjectFile(srcDict[dirRelPath], DataState.Added, dstData.ProjectPath);
                    ProjectFile dstDir = new ProjectFile(ProjectDataType.Directory);

                    ChangedFile newChange = new ChangedFile(srcDir, dstDir, DataState.Added);
                    fileChanges.Add(newChange);
                }

                foreach (string dirRelPath in dirsOnDst)
                {
                    ProjectFile srcFile = new ProjectFile(ProjectDataType.Directory);
                    ProjectFile dstFile = new ProjectFile(dstDict[dirRelPath], DataState.Deleted);
                    ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataState.Added);
                    fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Deleted));
                }

                foreach (string fileRelPath in filesOnSrc)
                {
                    ProjectFile srcFile = new ProjectFile(srcDict[fileRelPath], DataState.None);
                    ProjectFile dstFile = new ProjectFile(ProjectDataType.File);
                    fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Added, true));
                }

                foreach (string fileRelPath in filesOnDst)
                {
                    ProjectFile srcFile = new ProjectFile(ProjectDataType.File);
                    ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Deleted);
                    fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Deleted));
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

                IssueEventHandler?.Invoke(MetaDataState.Idle);
                return fileChanges;
            }
            catch (Exception ex)
            {
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run Find Version Differences Against Given Src");
                return null;
            }
        }
        #endregion

        #region Calls For File PreStage Update
        public void RetrieveDataSrc(string srcPath)
        {
            try
            {
                IssueEventHandler?.Invoke(MetaDataState.Retrieving);
                string[] binFiles = Directory.GetFiles(srcPath, "*.VersionLog", SearchOption.AllDirectories);
                if (binFiles.Length == 1)
                {
                    var stream = File.ReadAllBytes(binFiles[0]);
                    bool result = _fileHandlerTool.TryDeserializeProjectData(binFiles[0], out ProjectData? srcProjectData);
                    if (srcProjectData != null)
                    {
                        srcProjectData.ProjectPath = srcPath;
                        //srcProjectData.SetProjectFilesSrcPath();
                        //RegisterNewData(srcProjectData);
                        _srcProjectData = srcProjectData;
                        SrcProjectDataLoadedEventHandler?.Invoke(srcProjectData);
                        RegisterNewData(srcPath);
                    }
                }
                else
                {
                    RegisterNewData(srcPath);
                }
            }
            catch (Exception ex)
            {
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                MessageBox.Show($"File Manager RetrieveDataSrc Error: {ex.Message}");
            }
            IssueEventHandler?.Invoke(MetaDataState.Idle);
        }
        private void RegisterNewData(string srcDirPath)
        {
            string[]? filesAllDirectories;
            string[]? filesTopDirectories;
            string[]? dirsTopDirectories;
            string[]? dirsFullPaths;

            try
            {
                filesAllDirectories = Directory.GetFiles(srcDirPath, "*", SearchOption.AllDirectories);
                filesTopDirectories = Directory.GetFiles(srcDirPath, "*", SearchOption.TopDirectoryOnly);
                dirsTopDirectories = Directory.GetDirectories(srcDirPath, "*", SearchOption.TopDirectoryOnly);
                dirsFullPaths = Directory.GetDirectories(srcDirPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"FileManager RegisterNewData Error {ex.Message}");
                filesAllDirectories = null;
                filesTopDirectories = null;
                dirsTopDirectories = null;
                dirsFullPaths = null;
            }
            if (filesAllDirectories == null || filesTopDirectories == null || dirsFullPaths == null)
            {
                WPF.MessageBox.Show($"Couldn't get files or dirrectories from given Directory {srcDirPath}");
                return;
            }

            try
            {
                var filesSubDirectories = filesAllDirectories.Except(filesTopDirectories);
                GetAbnormalFiles(srcDirPath, filesTopDirectories.ToArray());

                foreach (string subDirFileAbsPath in filesSubDirectories)
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        new FileInfo(subDirFileAbsPath).Length,
                        FileVersionInfo.GetVersionInfo(subDirFileAbsPath).FileVersion,
                        Path.GetFileName(subDirFileAbsPath),
                        srcDirPath,
                        Path.GetRelativePath(srcDirPath, subDirFileAbsPath)
                        );
                    if (!_preStagedFilesDict.TryAdd(newFile.DataRelPath, newFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {newFile.DataName}: for Update");
                    }
                    else continue;
                }

                foreach (string dirAbsPath in dirsFullPaths)
                {
                    if (Path.GetExtension(dirAbsPath) == ".VersionLog") continue;
                    ProjectFile newFile = new ProjectFile
                        (
                        Path.GetFileName(dirAbsPath),
                        srcDirPath,
                        Path.GetRelativePath(srcDirPath, dirAbsPath)
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
        private void RegisterNewData(ProjectData srcProjectData)
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
        private void GetAbnormalFiles(string srcPath, string[] topDirFilePaths)
        {
            List<ChangedFile> registeredOverlapsList = new List<ChangedFile>();
            List<ChangedFile> registeredNewList = new List<ChangedFile>();

            for (int i = 0; i < topDirFilePaths.Length; i++)
            {
                int count = 0; 
                string? fileName = Path.GetFileName(topDirFilePaths[i]);
                List<ProjectFile>? overlappingFiles = null;
                if (fileName == null)
                {
                    MessageBox.Show($"Couldn't Process file name for overlapping file check on {topDirFilePaths[i]}");
                    return;
                }
                if (Path.GetExtension(topDirFilePaths[i]) == ".VersionLog") continue;
                foreach (ProjectFile file in _projectFilesDict.Values)
                {
                    if (file.DataName == fileName)
                    {
                        if (overlappingFiles == null) overlappingFiles = new List<ProjectFile>();
                        overlappingFiles.Add(file);
                        count++; 
                    }
                }
                // File Overlaps
                if (count >= 2 && overlappingFiles != null)
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        new FileInfo(topDirFilePaths[i]).Length,
                        FileVersionInfo.GetVersionInfo(topDirFilePaths[i]).FileVersion,
                        Path.GetFileName(topDirFilePaths[i]),
                        srcPath,
                        Path.GetRelativePath(srcPath, topDirFilePaths[i])
                        );
                    foreach (ProjectFile file in overlappingFiles)
                    {
                        ChangedFile newOverlap = new ChangedFile(newFile, new ProjectFile(file), DataState.Overlapped);
                        registeredOverlapsList.Add(newOverlap);
                    }
                }
                // Modification 
                else if (count == 1 && overlappingFiles != null)
                {
                    string newSrcFilePath = Path.Combine(srcPath, overlappingFiles[0].DataRelPath);
                    _fileHandlerTool.MoveFile(topDirFilePaths[i], newSrcFilePath);

                    ProjectFile newFile = new ProjectFile
                        (
                        new FileInfo(newSrcFilePath).Length,
                        FileVersionInfo.GetVersionInfo(newSrcFilePath).FileVersion,
                        Path.GetFileName(newSrcFilePath),
                        srcPath,
                        Path.GetRelativePath(srcPath, newSrcFilePath)
                        );
                    _preStagedFilesDict.TryAdd(newFile.DataRelPath, newFile);
                }
                // New File
                else
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        new FileInfo(topDirFilePaths[i]).Length,
                        FileVersionInfo.GetVersionInfo(topDirFilePaths[i]).FileVersion,
                        Path.GetFileName(topDirFilePaths[i]),
                        srcPath,
                        Path.GetRelativePath(srcPath, topDirFilePaths[i])
                        );
                    _preStagedFilesDict.TryAdd(newFile.DataRelPath, newFile);
                }
            }
            if (registeredOverlapsList.Count >= 1 || registeredNewList.Count >= 1)
            {
                OverlappedFileFoundEventHandler?.Invoke(registeredOverlapsList, registeredNewList);
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
        public void RegisterOverlapped(List<ChangedFile> sortedOverlaps, List<ChangedFile> sortedNew)
        {
            foreach (ChangedFile overlappedFile in sortedOverlaps)
            {
                if (overlappedFile.DstFile.IsDstFile)
                {
                    string newSrcFilePath = Path.Combine(overlappedFile.SrcFile.DataSrcPath, overlappedFile.DstFile.DataRelPath);
                    _fileHandlerTool.HandleFile(overlappedFile.SrcFile.DataAbsPath, newSrcFilePath, DataState.PreStaged);

                    ProjectFile newPreStagedFile = new ProjectFile(overlappedFile.SrcFile, DataState.PreStaged);
                    newPreStagedFile.DataRelPath = overlappedFile.DstFile.DataRelPath;
                    //Make new prestagedFile For computations 
                    _preStagedFilesDict.TryAdd(newPreStagedFile.DataRelPath, newPreStagedFile);
                }
            }
            foreach (ChangedFile newFile in sortedNew)
            {
                if (newFile.DstFile.IsDstFile)
                {
                    string newSrcFilePath = Path.Combine(newFile.SrcFile.DataSrcPath, newFile.DstFile.DataRelPath);
                    _fileHandlerTool.HandleFile(newFile.SrcFile.DataAbsPath, newSrcFilePath, DataState.PreStaged);

                    ProjectFile newPreStagedFile = new ProjectFile(newFile.SrcFile, DataState.PreStaged);
                    newPreStagedFile.DataRelPath = newFile.DstFile.DataRelPath;
                    //Make new prestagedFile For computations 
                    _preStagedFilesDict.TryAdd(newPreStagedFile.DataRelPath, newPreStagedFile);
                }
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
            IssueEventHandler?.Invoke(MetaDataState.Processing);
            await HashPreStagedFilesAsync();
            UpdateStageFileList();
            IssueEventHandler?.Invoke(MetaDataState.Idle);
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
                    Console.WriteLine(_asyncControl.CurrentCount);
                    if (file.DataType == ProjectDataType.Directory || file.DataHash != "") continue;
                    asyncTasks.Add(Task.Run(async () =>
                    {
                        await _asyncControl.WaitAsync();
                        try
                        {
                            _hashTool.GetFileMD5CheckSum(file);
                        }
                        finally
                        {
                            _asyncControl.Release();
                            Console.WriteLine(_asyncControl.CurrentCount);
                        }
                    }));
                }
                await Task.WhenAll(asyncTasks);
            }
            catch (Exception ex)
            {
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                WPF.MessageBox.Show($"File Manager UpdateHashFromChangedList Error: {ex.Message}");
            }
            finally
            {
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                _asyncControl.Release();
                Console.WriteLine(_asyncControl.CurrentCount);
            }
        }
        
        private void UpdateStageFileList()
        {
            if (_preStagedFilesDict.Count <= 0) return;

            foreach (ProjectFile registerdFile in _preStagedFilesDict.Values)
            {
                IssueEventHandler?.Invoke(MetaDataState.Processing);
                registerdFile.DataState &= ~DataState.PreStaged;
                //If File is being Restored
                if ((registerdFile.DataState & DataState.Restored) != 0 && registerdFile.DataType != ProjectDataType.Directory)
                {
                    _backupFilesDict.TryGetValue(registerdFile.DataHash, out ProjectFile? backupFile);
                    if (backupFile != null)
                    {
                        //File Modified 
                        if (_projectFilesDict.TryGetValue(backupFile.DataRelPath, out ProjectFile? projectFile))
                        {
                            if (backupFile.DataHash == projectFile.DataHash) continue;
                            ProjectFile srcFile = new ProjectFile(projectFile, DataState.Backup, backupFile.DataSrcPath);
                            ProjectFile dstFile = new ProjectFile(registerdFile, DataState.Modified, projectFile.DataSrcPath);
                            ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataState.Restored, true);
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
                    continue;
                    //Reset SrcPath to BackupPath
                }
                //If File is being Deleted
                if ((registerdFile.DataState & DataState.Deleted) != 0)
                {
                    ChangedFile newChange = new ChangedFile(new ProjectFile(registerdFile), DataState.Deleted);
                    _registeredChangesDict.TryAdd(registerdFile.DataRelPath, newChange);
                    continue;
                }
                // If File is Modified or Added
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
            IssueEventHandler?.Invoke(MetaDataState.Idle);
            DataStagedEventHandler?.Invoke(_registeredChangesDict.Values.ToList());
        }
        
        /// <summary>
        /// Clears StagedFiles Except those registered as IntegrityChecked
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
            SortProjectFilesByName(_projectFilesDict));
            DataStagedEventHandler?.Invoke(_registeredChangesDict.Values.ToList());
        }
        public void MetaDataLoadedCallBack(object metaDataObj)
        {
            if (metaDataObj is not ProjectMetaData projectMetaData) return;
            if (projectMetaData == null) return;
            this._backupFilesDict = projectMetaData.BackupFiles;
        }
        #endregion

        private void SortProjectFilesByName(Dictionary<string, ProjectFile> projectFilesDict)
        {
            _projectFilesDict_namesSorted.Clear();

            foreach (ProjectFile projFile in projectFilesDict.Values)
            {
                if (!_projectFilesDict_namesSorted.TryGetValue(projFile, out List<ProjectFile>? projFileList))
                {
                    _projectFilesDict_namesSorted.Add(projFile, new List<ProjectFile> { projFile });
                }
                _projectFilesDict_namesSorted[projFile].Add(projFile);
            }
        }

        #region Planned 
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
                foreach (ChangedFile changes in _registeredChangesDict.Values)
                {
                    if (changes.SrcFile == null && changes.DstFile != null) failedList.Add(changes.DstFile.DataName);

                }
                return (true, failedList);
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"{ex.Message}, Failed SrcFileIntegrityCheck On FileManager");
                return (true, null);
            }
        }

        public void RevertChange(ProjectFile file)
        {
            if ((file.DataState & DataState.IntegrityChecked) == 0) return;
            file.DataState &= ~DataState.IntegrityChecked;
            switch (file.DataState)
            {
                case DataState.Added:
                    _fileHandlerTool.HandleFile(null, file.DataAbsPath, DataState.Deleted);
                    break;
                case DataState.Modified:
                    if (!_projectFilesDict.TryGetValue(file.DataRelPath, out ProjectFile? projectFile_M))
                    {
                        MessageBox.Show("Couldn't revert change since recorded project file does not exist");
                        file.DataState |= DataState.IntegrityChecked;
                        return;
                    }
                    if (!_backupFilesDict.TryGetValue(projectFile_M.DataHash, out ProjectFile? backupFile_M))
                    {
                        MessageBox.Show("Couldn't revert change since backup does not exist");
                        file.DataState |= DataState.IntegrityChecked;
                        return;
                    }
                    _fileHandlerTool.HandleFile(backupFile_M.DataAbsPath, projectFile_M.DataAbsPath, DataState.Modified);
                    break;
                case DataState.Deleted:
                    if (!_projectFilesDict.TryGetValue(file.DataRelPath, out ProjectFile? projectFile_D))
                    {
                        MessageBox.Show("Coudln't Revert Change for Recorded Project file");
                        file.DataState |= DataState.IntegrityChecked;
                        return;
                    }
                    if (file.DataType == ProjectDataType.Directory)
                    {
                        _fileHandlerTool.HandleDirectory(null, projectFile_D.DataAbsPath, DataState.None);
                        break;
                    }
                    if (!_backupFilesDict.TryGetValue(projectFile_D.DataHash, out ProjectFile? backupFile_D))
                    {
                        MessageBox.Show("Couldn't revert change since backup does not exist");
                        file.DataState |= DataState.IntegrityChecked;
                        return;
                    }
                    _fileHandlerTool.HandleFile(backupFile_D.DataAbsPath, projectFile_D.DataAbsPath, DataState.Added);
                    break;
                default:
                    break;
            }

            if (_preStagedFilesDict.TryGetValue(file.DataRelPath, out ProjectFile? recordedPreFile))
                _preStagedFilesDict.Remove(file.DataRelPath);
            if (_registeredChangesDict.TryGetValue(file.DataRelPath, out ChangedFile? recordedChange))
                _registeredChangesDict.Remove(file.DataRelPath);

            DataStagedEventHandler?.Invoke(_registeredChangesDict.Values.ToList());
        }
        #endregion
    }
}