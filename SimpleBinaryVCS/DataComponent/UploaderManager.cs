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
        private ObservableCollection<ProjectFile> uploadedFileList; 
        public ObservableCollection<ProjectFile> UploadedFileList
        {
            get
            { 
                if (uploadedFileList == null) uploadedFileList = new ObservableCollection<ProjectFile>();
                return uploadedFileList;
            }
            set { uploadedFileList = value; }
        }

        private Queue<ChangedFile>? changedFileList;

        public UploaderManager()
        {
            uploadedFileList = new ObservableCollection<ProjectFile>();
            changedFileList = new Queue<ChangedFile>();

        }

        
        public void AddNewfile(ProjectFile fileUploaded)
        {
            uploadedFileList.Add(fileUploaded);
        }

        public void RemoveAll()
        {
            uploadedFileList.Clear();
        }
    }
}