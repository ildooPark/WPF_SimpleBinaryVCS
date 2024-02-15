using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using Microsoft.TeamFoundation.MVVM;
using WinForms = System.Windows.Forms;
using WPF = System.Windows; 
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
        private string? updateDirPath; 
        public ObservableCollection<ProjectFile> ChangedFileList { get; set; }
        private ProjectFile? selectedItem; 
        public ProjectFile? SelectedItem
        {
            get => selectedItem ??= null; 
            set
            {
                selectedItem = value;
                OnPropertyChanged("SelectedItem"); 
            }
        }
        private ICommand? clearNewfiles;
        public ICommand ClearNewfiles
        {
            get
            {
                clearNewfiles ??= new RelayCommand(ClearFiles, CanUploadFile);
                return clearNewfiles;
            }
        }

        private ICommand? getLocalChanges;
        public ICommand GetLocalChanges
        {
            get
            {
                getLocalChanges ??= new RelayCommand(PullLocalFileChanges, CanPullLocalChanges);
                return getLocalChanges;
            }
        }

        private ICommand? checkProjectIntegrity; 
        public ICommand CheckProjectIntegrity
        {
            get
            {
                checkProjectIntegrity ??= new RelayCommand(RunProjectVersionIntegrity, CanRunIntegrityTest); 
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
            fileManager = App.FileManager;
            this.ChangedFileList = fileManager.ChangedFileList;
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
            try
            {
                var openUpdateDir = new WinForms.FolderBrowserDialog();
                if (openUpdateDir.ShowDialog() == DialogResult.OK)
                {
                    updateDirPath = openUpdateDir.SelectedPath;
                }
                else
                {
                    openUpdateDir.Dispose();
                    return;
                }
                fileManager.RegisterNewFiles(updateDirPath);
                openUpdateDir.Dispose(); 
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show(ex.Message); 
            }
        }

        private void ClearFiles(object obj)
        {
            List<ProjectFile> clearList = new List<ProjectFile>();
            foreach (ProjectFile file in ChangedFileList)
            {
                if ((file.fileChangedState & FileChangedState.IntegrityChecked) == 0)
                {
                    clearList.Add(file);
                }
            }
            foreach (ProjectFile file in clearList)
            {
                ChangedFileList.Remove(file);
            }
            
        }

        private void PullLocalFileChanges(object parameter)
        {
            currentState = VMState.Calculating;
            //ChangedFile[]? changedFiles = fileManager.GetChangedFiles();
            currentState = VMState.Idle;
        }
        private bool CanRunIntegrityTest(object parameter)
        {
            return currentState == VMState.Idle;
        }
        private void RunProjectVersionIntegrity(object parameter)
        {
            currentState = VMState.Calculating;
            fileManager.PerformIntegrityCheck(parameter); 
            currentState = VMState.Idle;
        }
    }
}