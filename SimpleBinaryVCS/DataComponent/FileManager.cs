using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
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
        Changed,
        Deleted
    }
    public class FileManager
    {
        // Dependent on UploadManager, and VcsManager 
        private VersionControlManager vcsManager;
        private UploaderManager uploadManager;
        private Dictionary<string, FileBase> filesDict;
        private Queue<FileUploaded> changedFilesQueue; 
        // Collects Project File Changes, 
        // Activates the 
        public Action? fileChanges;
        public FileSystemWatcher? fileSystemWatcher { get; set; }
        public FileManager()
        {
            vcsManager = App.VcsManager;
            uploadManager = App.UploaderManager;
            filesDict = new Dictionary<string, FileBase>(); 
            changedFilesQueue = new Queue<FileUploaded>();
            vcsManager.projectLoadAction += ActivateFileWatcher;
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
            foreach (FileBase file in vcsManager.ProjectData.ProjectFiles)
            {
                filesDict.Add(file.fileName, file);
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        public FileBase[]? GetChangedFiles()
        {
            // Get Current ProjectFiles 
            if (changedFilesQueue.Count() == 0) return null; 
            // 
            FileBase[] changedFiles = new FileBase[changedFilesQueue.Count()];

            for (int i = 0; i < changedFilesQueue.Count(); i++)
            {
                changedFiles[i] = changedFilesQueue.Dequeue(); 
            }

            return changedFiles; 
        }

        public void RevertResponse(object obj)
        {

            filesDict.Clear(); 
        }

        private async Task<bool> PerformIntegrityCheck()
        {
            await Task.Run(() => RunIntegrityCheck());
            return true; 
        }

        private void RunIntegrityCheck()
        {
            
        }
    }
}
