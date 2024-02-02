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
        private string[] filesWithPath;
        private string[] filesNameOnly;
        public ObservableCollection<FileBase> UploadedFileList { get; set; }
        private FileBase selectedItem; 
        public FileBase SelectedItem
        {
            get { return selectedItem;}
            set
            {
                selectedItem = value;
                OnPropertyChanged("SelectedItem"); 
            }
        }
        private ICommand conductUpload;
        private ICommand clearUpload;
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
        public UploaderViewModel()
        {
            UploadedFileList = App.VcsManager.ProjectData.projectFiles;
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
            foreach (string filePath in filesWithPath)
            {

                var fileInfo = FileVersionInfo.GetVersionInfo(filePath);
                FileBase newFile = new FileBase(true, new FileInfo(filePath).Length, fileInfo.FileName, filePath, fileInfo.FileVersion);
                UploadedFileList.Add(newFile);
            }
        }

        private void RefreshFiles(object obj)
        {
            UploadedFileList.Clear(); 
        }
    }
}