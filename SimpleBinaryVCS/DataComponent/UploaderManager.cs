using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.DataComponent
{
    public class UploaderManager
    {
        private ObservableCollection<FileBase> uploadedFileList; 
        public ObservableCollection<FileBase> UploadedFileList
        {
            get
            { 
                if (uploadedFileList == null) uploadedFileList = new ObservableCollection<FileBase>();
                return uploadedFileList;
            }
            set { uploadedFileList = value; }
        }

        public UploaderManager()
        {
            uploadedFileList = new ObservableCollection<FileBase>();
        }

        public void AddNewfile(FileBase fileUploaded)
        {
            uploadedFileList.Add(fileUploaded);
        }

        public void RemoveAll()
        {
            uploadedFileList.Clear();
        }
    }
}