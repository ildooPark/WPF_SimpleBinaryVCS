using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using Microsoft.TeamFoundation.MVVM; 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;

namespace SimpleBinaryVCS.ViewModel
{
    public enum VMState
    {
        Idle,
        Calculating
    }
    public class FileTrackViewModel : ViewModelBase
    {
        private string[]? filesWithPath;
        private string[]? filesNameOnly;
        public ObservableCollection<ProjectFile> ChangedFileList { get; set; }
        private ProjectFile? selectedItem; 
        public ProjectFile SelectedItem
        {
            get { return selectedItem;}
            set
            {
                selectedItem = value;
                OnPropertyChanged("SelectedItem"); 
            }
        }
        private ICommand? conductUpload;
        private ICommand? clearUpload;
        public ICommand ConductUpload
        {
            get
            {
                if (conductUpload == null) conductUpload = new RelayCommand(UploadFile, CanUploadFile); 
                return conductUpload;
            }
        }
        public ICommand ClearUpload
        {
            get
            {
                if (clearUpload == null) clearUpload = new RelayCommand(RefreshFiles, CanUploadFile);
                return clearUpload;
            }
        }

        private ICommand? getLocalChanges;
        public ICommand GetLocalChanges
        {
            get
            {
                if (getLocalChanges == null) getLocalChanges = new RelayCommand(PullLocalFileChanges, CanPullLocalChanges);
                return getLocalChanges;
            }
        }

        private ICommand? checkProjectIntegrity; 
        public ICommand CheckProjectIntegrity
        {
            get
            {
                if (checkProjectIntegrity == null) checkProjectIntegrity = new RelayCommand(async (filesNameOnly) => await RunProjectVersionIntegrity(filesNameOnly), CanRunIntegrityTest); 
                return checkProjectIntegrity;
                    
            }
        }
        private int detectedFileChange;
        public int DetectedFileChange
        {
            get { return detectedFileChange; }
            set
            {
                detectedFileChange = value;
                OnPropertyChanged("DetectedFileChange");
            }
        }
        private FileManager fileManager;
        private VMState currentState; 
        public FileTrackViewModel()
        {
            this.ChangedFileList = App.FileTrackManager.ChangedFileList;
            fileManager = App.FileManager;
            fileManager.newLocalFileChange += OnNewLocalFileChange; 
            currentState = VMState.Idle; 
        }
        private void OnNewLocalFileChange(int numFile)
        {
            DetectedFileChange = numFile;
        }
        private bool CanPullLocalChanges(object obj) { return detectedFileChange != 0; }
        private bool CanUploadFile(object obj) { return true; }
        private void UploadFile(object obj)
        {
            OpenFileDialog fileOpen = new OpenFileDialog()
            {
                Multiselect = true
            };
            if (fileOpen.ShowDialog() == DialogResult.OK)
            {
                filesWithPath = fileOpen.FileNames;
                filesNameOnly = fileOpen.SafeFileNames;
            }
            else return; 
            for (int i = 0; i < filesWithPath.Length; i++)
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(filesWithPath[i]);
                ProjectFile newFile = new ProjectFile(
                    true, 
                    new FileInfo(filesWithPath[i]).Length, 
                    filesNameOnly[i], 
                    filesWithPath[i], 
                    fileInfo.FileVersion);
                newFile.fileChangedState = FileChangedState.Uploaded;
                ChangedFileList.Add(newFile);
            }
            fileOpen.Dispose(); 
        }

        private void RefreshFiles(object obj)
        {
            ChangedFileList.Clear(); 
        }

        private void PullLocalFileChanges(object parameter)
        {
            currentState = VMState.Calculating;
            ChangedFile[]? changedFiles = fileManager.GetChangedFiles();
            currentState = VMState.Idle;
        }
        private bool CanRunIntegrityTest(object parameter)
        {
            return currentState == VMState.Idle; 
        }
        private async Task RunProjectVersionIntegrity(object parameter)
        {
            currentState = VMState.Calculating;
            await fileManager.PerformIntegrityCheck(); 
            currentState = VMState.Idle;
        }
    }
}