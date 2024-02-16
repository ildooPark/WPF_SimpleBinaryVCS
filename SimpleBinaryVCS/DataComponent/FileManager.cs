using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.View;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using WPF = System.Windows;

namespace SimpleBinaryVCS.DataComponent
{
    [Flags]
    public enum FileChangedState
    {
        None = 0,
        Added = 1,
        Deleted = 1 << 1,
        Restored = 1 << 2,
        Modified = 1 << 3,
        Backup = 1 << 4,
        IntegrityChecked = 1 << 5
    }
    public class FileManager
    {
        // Dependent on UploadManager, and VcsManager 
        private bool fileChangeDetected; 
        public bool FileChangeDetected
        {
            get => fileChangeDetected;
            set => fileChangeDetected = value; 
        }
        private readonly object dictLock = new object();
        public Action? ClearFiles;
        public Action? requestFetchPull;
        public Action<int>? newLocalFileChange;
        public Action<object, string, ObservableCollection<ProjectFile>>? IntegrityCheckFinished;

        private Dictionary<string, ProjectFile> projectFilesDict;
        private Dictionary<string, ChangedFile> changedFilesDict;
        private SemaphoreSlim asyncControl;
        private ObservableCollection<ProjectFile> changedFileList;
        public ObservableCollection<ProjectFile> ChangedFileList
        {
            get => changedFileList ??= new ObservableCollection<ProjectFile>();
            set => changedFileList = value;
        }
        private VersionControlManager vcsManager;

        public FileManager()
        {
            vcsManager = App.VcsManager;
            changedFileList = new ObservableCollection<ProjectFile>();
            projectFilesDict = new Dictionary<string, ProjectFile>();
            changedFilesDict = new Dictionary<string, ChangedFile>();
            //changeNotifyTimer = new DispatcherTimer();
            //updateInterval = TimeSpan.FromSeconds(15);
            asyncControl = new SemaphoreSlim(5); 
            vcsManager.projectLoadAction += ActivateFileWatcher;
            vcsManager.updateAction += UpdateResponse; 
        }

        private void UpdateResponse(object obj)
        {
            return;
        }

        public void ActivateFileWatcher(object obj)
        {
            projectFilesDict.Clear(); 
            changedFilesDict.Clear();
            changedFileList.Clear();
            foreach (ProjectFile file in vcsManager.ProjectData.ProjectFiles)
            {
                projectFilesDict.Add(file.fileRelPath, file);
            }
        }

        private void UpdateChangedList()
        {
            foreach (ChangedFile file in changedFilesDict.Values)
            {
                //compare the hash value, and if its the same, request to remove that file. 
                if (projectFilesDict.TryGetValue(file.FileRelPath, out var correspondingFile))
                {
                    if (correspondingFile.fileHash != file.FileHash)
                    {
                        ProjectFile newFile = new ProjectFile(file, FileChangedState.Modified);
                        changedFileList.Add(newFile);
                    }
                    else continue;
                }
                else
                {
                    ProjectFile newFile = new ProjectFile(file, FileChangedState.Added);
                    changedFileList.Add(newFile);
                }
            }
        }

        /// <summary>
        /// Post Upload, Compute Hash value. 
        /// </summary>
        private async Task UpdateHashFromChangedList()
        {
            try
            {
                //Update changedFilesDict
                foreach (ChangedFile file in changedFilesDict.Values)
                {
                    await asyncControl.WaitAsync();
                    await vcsManager.GetFileMD5CheckSumAsync(file);
                    asyncControl.Release();
                }
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show(ex.Message);
            }
            
        }

        private async Task GetChangedFileHashAsync(ChangedFile file)
        {
            try
            {
                await asyncControl.WaitAsync();
                await vcsManager.GetFileMD5CheckSumAsync(file);
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
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
            RunIntegrityCheck(obj);
        }
        /// <summary>
        /// Based on File's given relative path to the project, 
        /// runs File Integrity Test against recorded Project Version to Current Project Directory files.
        /// </summary>
        /// <param name="obj"></param>
        private void RunIntegrityCheck(object obj)
        {
            changedFilesDict.Clear();
            changedFileList.Clear(); 
            try
            {
                StringBuilder fileIntegrityLog = new StringBuilder();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {vcsManager.ProjectData.updatedVersion}");
                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();

                foreach (ProjectFile file in projectFilesDict.Values)
                {
                    recordedFiles.Add(file.fileRelPath);
                }
                string[]? rawFiles = Directory.GetFiles(vcsManager.ProjectData.projectPath, "*", SearchOption.AllDirectories);
                foreach (string absPathFile in rawFiles)
                {
                    directoryFiles.Add(Path.GetRelativePath(vcsManager.ProjectData.projectPath, absPathFile));
                }
                IEnumerable<string> newlyAddedFiles = directoryFiles.Except(recordedFiles);
                IEnumerable<string> deletedFiles = recordedFiles.Except(directoryFiles);
                IEnumerable<string> intersectFiles = recordedFiles.Intersect(directoryFiles);

                foreach (string fileRelPath in newlyAddedFiles)
                {
                    if (fileRelPath == "VersionLog.bin") continue; 
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    //Add to the ChangedFileList 
                    string? fileHash = vcsManager.GetFileMD5CheckSum(vcsManager.ProjectData.projectPath, fileRelPath);
                    ProjectFile file = new ProjectFile(vcsManager.ProjectData.projectPath, fileRelPath, fileHash, FileChangedState.Added | FileChangedState.IntegrityChecked);
                    changedFileList.Add(file);
                }

                foreach (string fileRelPath in deletedFiles)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile file = projectFilesDict[fileRelPath];
                    file.fileChangedState = FileChangedState.Deleted | FileChangedState.IntegrityChecked;
                    changedFileList.Add(file);
                }
                foreach(string fileRelPath in intersectFiles)
                {
                    string? fileHash = vcsManager.GetFileMD5CheckSum(vcsManager.ProjectData.projectPath, fileRelPath);
                    if (projectFilesDict[fileRelPath].fileHash != fileHash)
                    {
                        fileIntegrityLog.AppendLine($"File {projectFilesDict[fileRelPath].fileName} on {fileRelPath} has been modified");
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(vcsManager.ProjectData.projectPath, fileRelPath));

                        ProjectFile file = new ProjectFile(projectFilesDict[fileRelPath]); 
                        file.fileChangedState = FileChangedState.Modified | FileChangedState.IntegrityChecked;
                        file.fileHash = fileHash;
                        file.isNew = true;
                        file.updatedTime = new FileInfo(file.fileFullPath()).LastAccessTime; 
                        changedFileList.Add(file);
                    }
                }
                fileIntegrityLog.AppendLine("Integrity Check Complete");
                IntegrityCheckFinished?.Invoke(obj, fileIntegrityLog.ToString(), changedFileList);
            }
            catch (Exception Ex)
            {
                System.Windows.MessageBox.Show($"{Ex.Message}. Couldn't Run File Integrity Check");
            }
        }

        public async void RegisterNewFiles(string updateDirPath)
        {
            string[]? filesFullPaths;
            try
            {
                filesFullPaths = Directory.GetFiles(updateDirPath, "*", SearchOption.AllDirectories);

            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
                filesFullPaths = null;
            }
            if (filesFullPaths == null)
            {
                WPF.MessageBox.Show($"Couldn't get files from given Directory {updateDirPath}");
                return;
            }

            try
            {
                foreach (string fileAbsPath in filesFullPaths)
                {
                    var fileInfo = FileVersionInfo.GetVersionInfo(fileAbsPath);
                    ChangedFile newFile = new ChangedFile(
                        FileChangedState.None,
                        updateDirPath,
                        Path.GetRelativePath(updateDirPath, fileAbsPath),
                        Path.GetFileName(fileAbsPath));

                    if (!changedFilesDict.TryAdd(newFile.FileRelPath, newFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {newFile.FileName}: for Update");
                    }
                    else continue; 
                }
            }
            catch (Exception Ex)
            {
                WPF.MessageBox.Show(Ex.Message);
                return;
            }
            await UpdateHashFromChangedList();
            UpdateChangedList();
            changedFilesDict.Clear();
        }

        public void RegisterNewfile(ProjectFile projectFile, FileChangedState state)
        {
            ProjectFile newfile = new ProjectFile(projectFile);
            newfile.isNew = true;
            newfile.fileChangedState = state;
            changedFileList.Add(newfile);
        }
    }
}
#region Deprecated 
#region FileSystemWatcher Deprecated 
//fileSystemWatcher = new FileSystemWatcher();
//if (vcsManager.ProjectData.projectPath != null)
//    fileSystemWatcher.Path = vcsManager.ProjectData.projectPath;
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
//        string fileRelPath = Path.GetRelativePath(vcsManager.ProjectData.projectPath, e.FullPath);
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
//                    vcsManager.ProjectData.projectPath,
//                    Path.GetRelativePath(vcsManager.ProjectData.projectPath, e.FullPath), 
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
//            await vcsManager.GetFileMD5CheckSumAsync(changedFilesDict[fileName]);
//            _FileChanges++;
//        }
//        else
//        {
//            changedFilesDict.Add(fileName, new ChangedFile(state, filePath, fileName));
//            await vcsManager.GetFileMD5CheckSumAsync(changedFilesDict[fileName]);
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
//        string fileRelPath = Path.GetRelativePath(vcsManager.ProjectData.projectPath, e.FullPath);
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
//                    vcsManager.ProjectData.projectPath,
//                    Path.GetRelativePath(vcsManager.ProjectData.projectPath, e.FullPath), 
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
//    //    vcsManager.GetMD5CheckSumAsync(registerHash);
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
//    vcsManager.GetMD5CheckSumAsync(newFile);
//    changedFilesQueue.Enqueue(newFile);
//    //vcsManager.ProjectData.ProjectFiles.Add(newFile);
//    //vcsManager.ProjectData.DiffLog.Add(newFile);
//}
//else
//{
//    //Compare Hash 
//    string? newFileHash = vcsManager.GetMD5CheckSum(filePath);
//    if (newFileHash == file.fileHash) return;
//    else
//    {
//        var fileInfo = FileVersionInfo.GetVersionInfo(e.FullPath);
//        ChangedFile newFile = new ChangedFile(
//            FileChangedState.Changed,
//            e.FullPath,
//            file.fileName,
//            newFileHash);

//        //vcsManager.GetMD5CheckSumAsync(newFile);
//        changedFilesQueue.Enqueue(newFile);
//    }
//    // Upload the file into Uploader Manager? 
//    // No, Directly Address to the DiffLog 
//    // 
//}
#endregion