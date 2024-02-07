using Microsoft.TeamFoundation.Build.Client;
using Microsoft.VisualBasic;
using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace SimpleBinaryVCS.DataComponent
{
    public enum FileChangedState
    {
        None,
        Added,
        Uploaded,
        Changed,
        Reverted,
        Deleted
    }

    public class FileManager
    {
        // Dependent on UploadManager, and VcsManager 
        private int _fileChanges;
        public int _FileChanges
        {
            get
            {
                return _fileChanges;
            }
            set
            {
                _fileChanges = Math.Max(value, 0);
                newLocalFileChange?.Invoke(_fileChanges);
            }
        }

        private bool fileChangeDetected; 
        public bool FileChangeDetected
        {
            get => fileChangeDetected; 
            set
            {
                fileChangeDetected = value;
            }
        }
        public Action<int>? newLocalFileChange;

        private VersionControlManager vcsManager;
        private FileTrackManager fileTrackManager;
        private Dictionary<string, ProjectFile> projectFilesDict;
        private Dictionary<string, ChangedFile> changedFilesDict;
        // Collects Project File Changes, 
        // Activates the 
        public Action? requestFetchPull;
        public FileSystemWatcher? fileSystemWatcher { get; set; }
        private DispatcherTimer changeNotifyTimer { get; set; } 
        private TimeSpan updateInterval { get; set; }

        public FileManager()
        {
            vcsManager = App.VcsManager;
            fileTrackManager = App.FileTrackManager;
            projectFilesDict = new Dictionary<string, ProjectFile>();
            changedFilesDict = new Dictionary<string, ChangedFile>();
            changeNotifyTimer = new DispatcherTimer();
            updateInterval = TimeSpan.FromSeconds(15);

            vcsManager.projectLoadAction += ActivateFileWatcher;
            vcsManager.updateAction += UpdateResponse; 
        }

        private void UpdateResponse(object obj)
        {

        }

        public void ActivateFileWatcher(object obj)
        {
            fileSystemWatcher = new FileSystemWatcher();
            if (App.VcsManager.ProjectData.projectPath != null)
                fileSystemWatcher.Path = App.VcsManager.ProjectData.projectPath;
            else
            {
                MessageBox.Show("Has Invalid ProjectPath");
                return;
            }
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.Created += OnFileCreated;
            fileSystemWatcher.Changed += OnFileChanged;
            fileSystemWatcher.Deleted += OnFileDeleted;
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.NotifyFilter =
                NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.LastWrite |
                NotifyFilters.FileName |
                NotifyFilters.Size; 

            changeNotifyTimer.Interval = updateInterval;
            changeNotifyTimer.Tick += OnTimerTicked;
            foreach (ProjectFile file in vcsManager.ProjectData.ProjectFiles)
            {
                changedFilesDict.Add(file.fileName, file);
            }
            
        }

        private void OnTimerTicked(object? sender, EventArgs e)
        {
            ////Gather all the files for comparing
            //if (changedFilesQueue.Count <= 0) return; 
            //for (int i = 0; i < changedFilesQueue.Count; i++)
            //{
            //    ChangedFile registerHash = changedFilesQueue.Dequeue();
            //    vcsManager.GetMD5CheckSumAsync(registerHash);
            //    _FileChanges--; 
            //}
        }

        private void ClearUnchangedFiles()
        {

        }

        private void OnRefresh()
        {
            // Manually Poll through the directory, 
            // if filename is new, || the lastWritetime 
        }

        private void StopTracker()
        {
            changeNotifyTimer?.Stop();
        }

        private void ResetTimer()
        {
            changeNotifyTimer?.Stop();
            changeNotifyTimer.Interval = updateInterval; 
            changeNotifyTimer?.Start(); 
        }

        private void RegisterChangesWithoutOverlap(FileChangedState state, string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string fileName = Path.GetFileName(filePath);
                if (changedFilesDict.ContainsKey(fileName))
                {
                    TimeSpan timeDiff = changedFilesDict[fileName].lastRead - fileInfo.LastWriteTime;
                    if (timeDiff.TotalSeconds < 1) return;
                    changedFilesDict[fileName] = new ChangedFile(FileChangedState.Changed, filePath, fileName);
                    vcsManager.GetMD5CheckSumAsync(changedFilesDict[fileName]);
                    _FileChanges++;
                }
                else
                {
                    changedFilesDict.Add(fileName, new ChangedFile(state, filePath, fileName));
                    vcsManager.GetMD5CheckSumAsync(changedFilesDict[fileName]);
                    _FileChanges++; 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}");
            }
        }
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            ChangedFile detectedFile = new ChangedFile(FileChangedState.Deleted, e.FullPath, Path.GetFileName(e.FullPath));
            string filePath = e.FullPath;
        }


        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // If file already Exists, and the delta second between last read time and last written time 
            try
            {
                FileInfo fileInfo = new FileInfo(e.FullPath);
                string fileName = Path.GetFileName(e.FullPath);
                if (changedFilesDict.ContainsKey(fileName))
                {
                    TimeSpan timeDiff = changedFilesDict[fileName].lastRead - fileInfo.LastWriteTime;
                    if (timeDiff.TotalSeconds < 1) return;
                    changedFilesDict[fileName] = new ChangedFile(FileChangedState.Changed, e.FullPath, fileName);
                    vcsManager.GetMD5CheckSumAsync(changedFilesDict[fileName]);
                    _FileChanges++; 
                }
            }
            catch (Exception ex)
            {

            }

            ChangedFile changedFile = new ChangedFile(FileChangedState.Changed, e.FullPath, Path.GetFileName(e.FullPath));
            _FileChanges++; 
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
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        public ChangedFile[]? GetChangedFiles()
        {
            //// Get Current ProjectFiles 
            //if (changedFilesQueue.Count() == 0) return null; 
            //// 
            //ChangedFile[] changedFiles = new ChangedFile[changedFilesQueue.Count()];

            //for (int i = 0; i < changedFilesQueue.Count(); i++)
            //{
            //    changedFiles[i] = changedFilesQueue.Dequeue(); 
            //}

            //return changedFiles; 
        }

        public void RevertResponse(object obj)
        {

            changedFilesDict.Clear(); 
        }

        public async Task PerformIntegrityCheck()
        {
            await Task.Run(() => RunIntegrityCheck());
            return;
        }

        private void RunIntegrityCheck()
        {
            
        }
    }
}
