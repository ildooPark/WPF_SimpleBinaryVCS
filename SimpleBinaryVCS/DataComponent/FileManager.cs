using Microsoft.TeamFoundation.Build.Client;
using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace SimpleBinaryVCS.DataComponent
{
    public enum FileChangedState
    {
        Added,
        Uploaded,
        Changed,
        Deleted
    }
    public class FileManager
    {
        // Dependent on UploadManager, and VcsManager 
        private VersionControlManager vcsManager;
        private UploaderManager uploadManager;
        private Dictionary<string, ProjectFile> changedFilesDict;
        private Queue<ChangedFile> changedFilesQueue; 
        // Collects Project File Changes, 
        // Activates the 
        public Action? fileChanges;
        public FileSystemWatcher? fileSystemWatcher { get; set; }
        public FileManager()
        {
            vcsManager = App.VcsManager;
            uploadManager = App.UploaderManager;
            changedFilesDict = new Dictionary<string, ProjectFile>(); 
            changedFilesQueue = new Queue<ChangedFile>();
            vcsManager.projectLoadAction += ActivateFileWatcher;
            vcsManager.updateAction += UpdateResponse; 
        }

        private void UpdateResponse(object obj)
        {
            throw new NotImplementedException();
        }

        public void ActivateFileWatcher(object obj)
        {
            using var _fileSystemWatcher = new FileSystemWatcher();
            if (_fileSystemWatcher == null) { MessageBox.Show("Couldn't Establish FileSystemWatcher"); return; }
            fileSystemWatcher = _fileSystemWatcher;
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
            foreach (ProjectFile file in vcsManager.ProjectData.ProjectFiles)
            {
                changedFilesDict.Add(file.fileName, file);
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            string filePath = e.FullPath;
            bool checkExistingFile = changedFilesDict.TryGetValue(Path.GetFileName(e.FullPath), out var file); 
            if (!checkExistingFile)
            {
                MessageBox.Show($"Following file {Path.GetFileName(e.FullPath)} is not Changed File!"); return; 
                //Make new File
                var fileInfo = FileVersionInfo.GetVersionInfo(e.FullPath);
                ProjectFile newFile = new ProjectFile(
                    true,
                    new FileInfo(e.FullPath).Length,
                    Path.GetFileName(e.FullPath),
                    e.FullPath,
                    fileInfo.FileVersion);
                newFile.fileHash = App.VcsManager.GetMD5CheckSum(e.FullPath);
                newFile.deployedProjectVersion = vcsManager.ProjectData.updatedVersion;
                vcsManager.ProjectData.ProjectFiles.Add(newFile);
                vcsManager.ProjectData.DiffLog.Add(newFile);
            }
            else
            {
                //Compare Hash 
                string? newFileHash = vcsManager.GetMD5CheckSum(filePath);
                if (newFileHash == file.fileHash) return;
                // Upload the file into Uploader Manager? 
                // No, Directly Address to the DiffLog 
                // 
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        public ProjectFile[]? GetChangedFiles()
        {
            // Get Current ProjectFiles 
            if (changedFilesQueue.Count() == 0) return null; 
            // 
            ProjectFile[] changedFiles = new ProjectFile[changedFilesQueue.Count()];

            for (int i = 0; i < changedFilesQueue.Count(); i++)
            {
                changedFiles[i] = changedFilesQueue.Dequeue(); 
            }

            return changedFiles; 
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
