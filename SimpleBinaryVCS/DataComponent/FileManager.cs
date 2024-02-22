using MemoryPack;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using WPF = System.Windows;

namespace SimpleBinaryVCS.DataComponent
{
    [Flags]
    public enum DataChangedState
    {
        None = 0,
        Added = 1,
        Deleted = 1 << 1,
        Restored = 1 << 2,
        Modified = 1 << 3,
        UnStaged = 1 << 4,
        IntegrityChecked = 1 << 5
    }
    public class FileManager : IManager
    {
        private readonly object dictLock = new object();
        public Action<object>? UpdateChanges;
        public Action<object>? UpdateChangesObservable;
        public Action<object>? SrcProjectDeployed;
        public Action<object, string, ObservableCollection<ProjectFile>>? IntegrityCheckFinished;

        private Dictionary<string, ProjectFile> projectFilesDict;
        private Dictionary<string, ProjectFile> changedFilesDict;
        private SemaphoreSlim asyncControl;
        private ObservableCollection<ProjectFile> changedFileList;
        public ObservableCollection<ProjectFile> ChangedFileList
        {
            get => changedFileList ??= new ObservableCollection<ProjectFile>();
            set => changedFileList = value;
        }

        private ProjectData? currentProjectData;
        public ProjectData CurrentProjectData
        {
            get
            {
                if (currentProjectData == null)
                {
                    MessageBox.Show("Project Data is unreachable from FileManager");
                    return new ProjectData(); 
                }
                return currentProjectData;
            }
        }
        private FileHandlerTool fileHandlerTool;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public FileManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            changedFileList = new ObservableCollection<ProjectFile>();
            projectFilesDict = new Dictionary<string, ProjectFile>();
            changedFilesDict = new Dictionary<string, ProjectFile>();
            fileHandlerTool = new FileHandlerTool();    
            asyncControl = new SemaphoreSlim(5); 
        }

        public void Awake()
        {
        }
        public void Start(object projObj)
        {
            if (projObj is not ProjectData loadedProject) return;

            changedFilesDict.Clear();
            changedFileList.Clear();
            currentProjectData = loadedProject;
            projectFilesDict = currentProjectData.ProjectFilesDict;
        }

        private void UpdateResponse(object obj)
        {
            return;
        }

        private void UpdateChangedList()
        {
            if (changedFilesDict.Count <= 0) return;    
            foreach (ProjectFile file in changedFilesDict.Values)
            {
                //compare the hash value, and if its the same, request to remove that file. 
                if (projectFilesDict.TryGetValue(file.DataRelPath, out var correspondingFile))
                {
                    if (correspondingFile.DataHash != file.DataHash)
                    {
                        ProjectFile newFile = new ProjectFile(file, DataChangedState.Modified);
                        changedFileList.Add(newFile);
                    }
                    else continue;
                }
                else
                {
                    ProjectFile newFile = new ProjectFile(file, DataChangedState.Added);
                    changedFileList.Add(newFile);
                }
            }
            UpdateChangesObservable?.Invoke(changedFileList);
        }

        /// <summary>
        /// Post Upload, Compute Hash value. 
        /// </summary>
        private async Task UpdateHashFromChangedList()
        {
            try
            {
                if (changedFilesDict.Count <= 0) return;
                List<Task> asyncTasks = new List<Task>();
                //Update changedFilesDict
                foreach (ProjectFile file in changedFilesDict.Values)
                {
                    if (file.DataType == ProjectDataType.Directory) continue;
                    await asyncControl.WaitAsync();
                    try
                    {
                        Task task = HashTool.GetFileMD5CheckSumAsync(file);
                        asyncTasks.Add(task);
                        await task;
                    }
                    finally
                    {
                        asyncControl.Release();
                    }
                }
                await Task.WhenAll(asyncTasks);
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show(ex.Message);
            }
            finally
            {
                asyncControl.Release();
            }
            
        }

        public void RevertResponse(object obj)
        {

            changedFilesDict.Clear(); 
        }

        public void PerformIntegrityCheck(object obj)
        {
            MainProjectIntegrityCheck(obj);
        }

        /// <summary>
        /// Based on File's given relative path to the project, 
        /// runs File Integrity Test against recorded Project Version to Current Project Directory files.
        /// </summary>
        /// <param name="obj"></param>
        private void MainProjectIntegrityCheck(object obj)
        {
            ProjectData? mainProject = currentProjectData;
            if (mainProject == null)
            {
                MessageBox.Show("Main Project is Missing");
                return;
            }
            changedFilesDict.Clear();
            changedFileList.Clear(); 
            try
            {
                StringBuilder fileIntegrityLog = new StringBuilder();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {mainProject.UpdatedVersion}");

                Dictionary<string, ProjectFile> projectFilesDict = mainProject.ProjectFilesDict;

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
                foreach (string absPathFile in rawFiles)
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

                }

                foreach (string dirRelPath in deletedDirs)
                {

                }

                foreach (string fileRelPath in addedFiles)
                {
                    if (fileRelPath == "VersionLog.bin") continue; 
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(mainProject.ProjectPath, fileRelPath);
                    ProjectFile file = new ProjectFile(mainProject.ProjectPath, fileRelPath, fileHash, DataChangedState.Added | DataChangedState.IntegrityChecked);
                    changedFileList.Add(file);
                }

                foreach (string fileRelPath in deletedFiles)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile file = projectFilesDict[fileRelPath];
                    file.DataState = DataChangedState.Deleted | DataChangedState.IntegrityChecked;
                    changedFileList.Add(file);
                }

                foreach(string fileRelPath in intersectFiles)
                {
                    string? dirfileHash = HashTool.GetFileMD5CheckSum(mainProject.ProjectPath, fileRelPath);
                    if (projectFilesDict[fileRelPath].DataHash != dirfileHash)
                    {
                        fileIntegrityLog.AppendLine($"File {projectFilesDict[fileRelPath].DataName} on {fileRelPath} has been modified");
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(mainProject.ProjectPath, fileRelPath));

                        ProjectFile file = new ProjectFile(projectFilesDict[fileRelPath], DataChangedState.Modified | DataChangedState.IntegrityChecked); 
                        file.DataState = DataChangedState.Modified | DataChangedState.IntegrityChecked;
                        file.DataHash = dirfileHash;
                        file.UpdatedTime = new FileInfo(file.DataAbsPath).LastAccessTime; 
                        changedFileList.Add(file);
                    }
                }
                fileIntegrityLog.AppendLine("Integrity Check Complete");
                IntegrityCheckFinished?.Invoke(obj, fileIntegrityLog.ToString(), changedFileList);
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
        public List<ChangedFile>? FindVersionDifferences(ProjectData srcData, ProjectData dstData, bool isRevert = true)
        {
            try
            {
                List<ChangedFile> diffLog = new List<ChangedFile>();

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
                    ProjectFile srcFile = new ProjectFile(srcDict[dirRelPath]);
                    ProjectFile dstFile = new ProjectFile(srcDict[dirRelPath], DataChangedState.Added, dstData.ProjectPath);
                    ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataChangedState.Added);
                    diffLog.Add(newChange);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    ProjectFile file = new ProjectFile(dstDict[dirRelPath], DataChangedState.Deleted);
                    diffLog.Add(new ChangedFile(file, DataChangedState.Deleted));
                }
                //2. Files 
                foreach (string fileRelPath in filesToAdd)
                {
                    ProjectFile file = new ProjectFile(srcDict[fileRelPath], DataChangedState.Added);
                    diffLog.Add(new ChangedFile(file, DataChangedState.Added));
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    ProjectFile file = new ProjectFile(dstDict[fileRelPath], DataChangedState.Deleted);
                    diffLog.Add(new ChangedFile(file, DataChangedState.Deleted));
                }

                foreach (string fileRelPath in intersectFiles)
                {
                    if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
                    {
                        ProjectFile srcFile = new ProjectFile(srcDict[fileRelPath]);
                        ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataChangedState.Modified);
                        diffLog.Add(new ChangedFile(srcFile, dstFile, DataChangedState.Modified));
                    }
                }

                UpdateChangesObservable?.Invoke(diffLog);
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
            if (srcData == null || dstData == null)
            {
                MessageBox.Show($"One or more project is set to null");
                return;
            }
            try
            {
                List<ChangedFile> diffLog = new List<ChangedFile>();
                StringBuilder fileIntegrityLog = new StringBuilder();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {dstData.UpdatedVersion}");
                Dictionary<string, ProjectFile> srcDict = srcData.ProjectFilesDict;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFilesDict;

                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();

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

                foreach (string dirRelPath in dirsToAdd)
                {
                    ProjectFile srcFile = new ProjectFile(srcDict[dirRelPath]);
                    ProjectFile dstFile = new ProjectFile(srcDict[dirRelPath], DataChangedState.Added, dstData.ProjectPath);
                    ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataChangedState.Added);
                    diffLog.Add(newChange);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    ProjectFile dstFile = new ProjectFile(dstDict[dirRelPath], DataChangedState.Deleted);
                    changeList.Add(dstFile);
                }

                foreach (string fileRelPath in filesToAdd)
                {
                    ProjectFile srcFile = new ProjectFile(srcDict[fileRelPath]);
                    ProjectFile dstFile = new ProjectFile(srcDict[fileRelPath], DataChangedState.Added, dstData.ProjectPath);
                    ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataChangedState.Added);
                    diffLog.Add(newChange);
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataChangedState.Deleted);
                    ChangedFile newChange = new ChangedFile(dstFile, DataChangedState.Deleted);
                    diffLog.Add(newChange);
                }

                foreach (string fileRelPath in intersectFiles)
                {
                    if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
                    {
                        ProjectFile dstFile = new ProjectFile(srcDict[fileRelPath], DataChangedState.Modified);
                        ProjectFile srcFile = new ProjectFile(srcDict[fileRelPath]);
                        ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataChangedState.Modified);
                        dstData.ChangedFiles.Add(newChange);
                    }
                }

                fileIntegrityLog.AppendLine("Integrity Check Complete");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run File Integrity Check");
            }
        }
        public void RetrieveDataSrc(string srcPath)
        {
            try
            {
                string[] binFiles = Directory.GetFiles(srcPath, "VersionLog.*", SearchOption.AllDirectories);
                if (binFiles.Length >= 1)
                {
                    var stream = File.ReadAllBytes(binFiles[0]);
                    ProjectData? srcProjectData = MemoryPackSerializer.Deserialize<ProjectData>(stream);
                    if (srcProjectData != null)
                    {
                        ChangedFileList.Clear();
                        srcProjectData.ProjectPath = srcPath;
                        srcProjectData.SetProjectFilesSrcPath();
                        RegisterNewData(srcProjectData);
                        List<ChangedFile>? changedFiles = FindVersionDifferences(srcProjectData, currentProjectData);
                        UpdateChanges?.Invoke(changedFiles);
                    }
                }
                else
                {
                    RegisterNewData(srcPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                MessageBox.Show(ex.Message);
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
                        ProjectDataType.File,
                        new FileInfo(fileAbsPath).Length,
                        FileVersionInfo.GetVersionInfo(fileAbsPath).FileVersion,
                        null,
                        null,
                        DataChangedState.UnStaged,
                        Path.GetFileName(fileAbsPath),
                        updateDirPath,
                        Path.GetRelativePath(updateDirPath, fileAbsPath),
                        null
                        );

                    if (!changedFilesDict.TryAdd(newFile.DataRelPath, newFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {newFile.DataName}: for Update");
                    }
                    else continue; 
                }
                foreach (string dirAbsPath in dirsFullPaths)
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        ProjectDataType.Directory,
                        0,
                        null,
                        null,
                        null,
                        DataChangedState.UnStaged,
                        Path.GetFileName(dirAbsPath),
                        updateDirPath,
                        Path.GetRelativePath(updateDirPath, dirAbsPath),
                        null
                        );

                    if (!changedFilesDict.TryAdd(newFile.DataRelPath, newFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {newFile.DataName}: for Update");
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
        public void RegisterNewData(ProjectData srcProjectData)
        {
            try
            {
                foreach (ProjectFile srcFile in srcProjectData.ProjectFiles)
                {
                    srcFile.DataState |= DataChangedState.UnStaged;
                    if (!changedFilesDict.TryAdd(srcFile.DataRelPath, srcFile))
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

        public ObservableCollection<ProjectFile> DataChangesObservable (List<ChangedFile> dataChanges)
        {
            ObservableCollection<ProjectFile> changedFilesObservable = new ObservableCollection<ProjectFile>();
            foreach (ChangedFile changes in dataChanges)
            {
                if (changes.DstFile != null) changedFilesObservable.Add(changes.DstFile);
                if (changes.SrcFile != null) changedFilesObservable.Add(changes.SrcFile);
            }
            return changedFilesObservable;
        }

        public async void StageNewFiles()
        {
            await UpdateHashFromChangedList();
            UpdateChangedList();
            changedFilesDict.Clear();
        }

        public void RegisterNewfile(ProjectFile projectFile, DataChangedState fileState)
        {
            ProjectFile newfile = new ProjectFile(projectFile);
            newfile.DataState = fileState;
            changedFileList.Add(newfile);
        }
    }
}
#region Deprecated 
#region FileSystemWatcher Deprecated 
//fileSystemWatcher = new FileSystemWatcher();
//if (metaDataManager.ProjectData.projectPath != null)
//    fileSystemWatcher.Path = metaDataManager.ProjectData.projectPath;
//else
//{
//    System.Windows.MessageBox.Show("Has Invalid ProjectPath");
//    return;
//}
//fileSystemWatcher.IncludeSubdirectories = true;
//fileSystemWatcher.Created += OnFileCreated;
//fileSystemWatcher.Changed += OnFileChanged;
//fileSystemWatcher.Deleted += OnFileDeleted;
//fileSystemWatcher.EnableRaisingEvents = true;
//fileSystemWatcher.NotifyFilter =
//    NotifyFilters.Attributes |
//    NotifyFilters.CreationTime |
//    NotifyFilters.LastWrite |
//    NotifyFilters.FileName |
//    NotifyFilters.Size; 

//changeNotifyTimer.Interval = updateInterval;
//changeNotifyTimer.Tick += OnTimerTicked;
#endregion
//public FileSystemWatcher? fileSystemWatcher { get; set; }
//private DispatcherTimer changeNotifyTimer { get; set; } 
//private TimeSpan updateInterval { get; set; }
//private void OnFileCreated(object sender, FileSystemEventArgs e)
//{
//    return;
//}
//private void OnFileDeleted(object sender, FileSystemEventArgs e)
//{
//    try
//    {
//        string fileRelPath = Path.GetRelativePath(metaDataManager.ProjectData.projectPath, e.FullPath);
//        if (projectFilesDict.ContainsKey(fileRelPath))
//        {
//            if (changedFilesDict.ContainsKey(fileRelPath))
//            {
//                //If changeFileDict contains
//                TimeSpan timeDiff = changedFilesDict[fileRelPath].changedTime - DateTime.Now;
//                if (Math.Abs(timeDiff.TotalSeconds) < 1) return;
//                changedFilesDict[fileRelPath].changedTime = DateTime.Now;
//                changedFilesDict[fileRelPath].fileChangedState = FileChangedState.Deleted;

//            }
//            else
//            {
//                changedFilesDict.Add(fileRelPath, 
//                    new ChangedFile
//                    (FileChangedState.Deleted,
//                    metaDataManager.ProjectData.projectPath,
//                    Path.GetRelativePath(metaDataManager.ProjectData.projectPath, e.FullPath), 
//                    Path.GetFileName(e.FullPath)));
//                _fileChanges++;
//            }
//        }
//        else return; // File that was not registered as left, considered as insignificant change.
//    }
//    catch (Exception ex)
//    {
//        System.Windows.MessageBox.Show($"{ex.Message}");
//    }
//}

//private async void RegisterChangesWithoutOverlap(FileChangedState state, string filePath)
//{
//    try
//    {
//        FileInfo fileInfo = new FileInfo(filePath);
//        string fileName = Path.GetFileName(filePath);
//        if (changedFilesDict.ContainsKey(fileName))
//        {
//            TimeSpan timeDiff = changedFilesDict[fileName].changedTime - fileInfo.LastWriteTime;
//            if (timeDiff.TotalSeconds < 1) return;
//            changedFilesDict[fileName] = new ChangedFile(FileChangedState.Modified, filePath, fileName);
//            await metaDataManager.GetFileMD5CheckSumAsync(changedFilesDict[fileName]);
//            _FileChanges++;
//        }
//        else
//        {
//            changedFilesDict.Add(fileName, new ChangedFile(state, filePath, fileName));
//            await metaDataManager.GetFileMD5CheckSumAsync(changedFilesDict[fileName]);
//            _FileChanges++; 
//        }
//    }
//    catch (Exception ex)
//    {
//        System.Windows.MessageBox.Show($"{ex.Message}");
//    }
//}

//private async void OnFileChanged(object sender, FileSystemEventArgs e)
//{
//    // If file already Exists, and the delta second between last read time and last written time 
//    try
//    {
//        FileInfo fileInfo = new FileInfo(e.FullPath);
//        string fileRelPath = Path.GetRelativePath(metaDataManager.ProjectData.projectPath, e.FullPath);
//        if (changedFilesDict.ContainsKey(fileRelPath))
//        {
//            TimeSpan timeDiff = changedFilesDict[fileRelPath].changedTime - DateTime.Now;
//            if (Math.Abs(timeDiff.TotalSeconds) < 1) return;

//            await asyncControl.WaitAsync();
//            string? hash = await GetHashAsync(fileRelPath);
//            if (hash == null) return; 
//            lock (dictLock)
//            {
//                changedFilesDict[fileRelPath].fileChangedState = FileChangedState.Modified;
//                changedFilesDict[fileRelPath].FileHash = hash; 
//                _FileChanges++;
//            }
//        }
//        else
//        {
//            changedFilesDict.Add(fileRelPath, 
//                new ChangedFile(
//                    FileChangedState.Modified, 
//                    metaDataManager.ProjectData.projectPath,
//                    Path.GetRelativePath(metaDataManager.ProjectData.projectPath, e.FullPath), 
//                    Path.GetFileName(e.FullPath)));
//            await asyncControl.WaitAsync();
//            changedFilesDict[fileRelPath].FileHash = await GetHashAsync(fileRelPath);
//            _FileChanges++;
//        }
//    }
//    catch (Exception ex)
//    {
//        System.Windows.MessageBox.Show($"{ex.Message}");
//    }

//}

//private void StopTracker()
//{
//    changeNotifyTimer?.Stop();
//}

//private void ResetTimer()
//{
//    changeNotifyTimer?.Stop();
//    changeNotifyTimer.Interval = updateInterval;
//    changeNotifyTimer?.Start();
//}
//private void OnTimerTicked(object? sender, EventArgs e)
//{
//    ////Gather all the files for comparing
//    //if (changedFilesQueue.Count <= 0) return; 
//    //for (int i = 0; i < changedFilesQueue.Count; i++)
//    //{
//    //    ChangedFile registerHash = changedFilesQueue.Dequeue();
//    //    metaDataManager.GetMD5CheckSumAsync(registerHash);
//    //    _FileChanges--; 
//    //}
//}

//bool checkExistingFile = changedFilesDict.TryGetValue(Path.GetFileName(e.FullPath), out var file); 
//if (!checkExistingFile)
//{
//    MessageBox.Show($"Following file {Path.GetFileName(e.FullPath)} is not Changed File!");                
//    //Make new File
//    var fileInfo = FileVersionInfo.GetVersionInfo(e.FullPath);
//    ChangedFile newFile = new ChangedFile(
//        FileChangedState.Changed,
//        e.FullPath,
//        Path.GetFileName(e.FullPath));
//    metaDataManager.GetMD5CheckSumAsync(newFile);
//    changedFilesQueue.Enqueue(newFile);
//    //metaDataManager.ProjectData.ProjectFiles.Add(newFile);
//    //metaDataManager.ProjectData.DiffLog.Add(newFile);
//}
//else
//{
//    //Compare Hash 
//    string? newFileHash = metaDataManager.GetMD5CheckSum(filePath);
//    if (newFileHash == file.fileHash) return;
//    else
//    {
//        var fileInfo = FileVersionInfo.GetVersionInfo(e.FullPath);
//        ChangedFile newFile = new ChangedFile(
//            FileChangedState.Changed,
//            e.FullPath,
//            file.fileName,
//            newFileHash);

//        //metaDataManager.GetMD5CheckSumAsync(newFile);
//        changedFilesQueue.Enqueue(newFile);
//    }
//    // Upload the file into Uploader Manager? 
//    // No, Directly Address to the DiffLog 
//    // 
//}
#endregion