using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.DataComponent
{
    
    public class UploaderManager
    {
        public Action<object>? UploadTrigger;
        public Action<object>? fileChangeTrigger;
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

        private Queue<FileUploaded>? changedFileList;

        public UploaderManager()
        {
            uploadedFileList = new ObservableCollection<FileBase>();
            changedFileList = new Queue<FileUploaded>();

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