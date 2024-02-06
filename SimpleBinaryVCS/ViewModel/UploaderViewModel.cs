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
    public class UploaderViewModel : ViewModelBase
    {
        private string[]? filesWithPath;
        private string[]? filesNameOnly;
        public ObservableCollection<FileBase> UploadedFileList { get; set; }
        private FileBase? selectedItem; 
        public FileBase SelectedItem
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


        private FileManager fileManager;
        public UploaderViewModel()
        {
            UploadedFileList = App.UploaderManager.UploadedFileList;
            fileManager = App.FileManager; 
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
                FileBase newFile = new FileBase(
                    true, 
                    new FileInfo(filesWithPath[i]).Length, 
                    filesNameOnly[i], 
                    filesWithPath[i], 
                    fileInfo.FileVersion);
                UploadedFileList.Add(newFile);
            }

            fileOpen.Dispose(); 
        }

        private void RefreshFiles(object obj)
        {
            UploadedFileList.Clear(); 
        }
    }
}