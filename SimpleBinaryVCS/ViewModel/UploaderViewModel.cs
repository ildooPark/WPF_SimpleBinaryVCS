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
    public class UploaderViewModel : ViewModelBase
    {
        private string[]? filesWithPath;
        private string[]? filesNameOnly;
        public ObservableCollection<ProjectFile> UploadedFileList { get; set; }
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

        private ICommand? refreshProject;
        public ICommand RefreshProject
        {
            get
            {
                if (refreshProject == null) refreshProject = new RelayCommand(RefreshProjectDirectory, CanUploadFile);
                return refreshProject;
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
        private FileManager fileManager;
        private VMState currentState; 
        public UploaderViewModel()
        {
            UploadedFileList = App.UploaderManager.UploadedFileList;
            fileManager = App.FileManager;
            currentState = VMState.Idle; 
        }

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
                UploadedFileList.Add(newFile);
            }

            fileOpen.Dispose(); 
        }

        private void RefreshFiles(object obj)
        {
            UploadedFileList.Clear(); 
        }

        private void RefreshProjectDirectory(object parameter)
        {
            ProjectFile[]? changedFiles = fileManager.GetChangedFiles(); 
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