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
    
    public class FileTrackManager
    {
        public Action<object>? UploadTrigger;
        public Action<object>? fileChangeTrigger;
        private ObservableCollection<ProjectFile> changedFileList; 
        public ObservableCollection<ProjectFile> ChangedFileList
        {
            get
            { 
                if (changedFileList == null) changedFileList = new ObservableCollection<ProjectFile>();
                return changedFileList;
            }
            set { changedFileList = value; }
        }

        public Dictionary<string, ProjectFile> changedFileListDict;
        private Queue<TrackedFile> changedFileListQueue;

        public FileTrackManager()
        {
            changedFileList = new ObservableCollection<ProjectFile>();
            changedFileListQueue = new Queue<TrackedFile>();
            changedFileListDict = new Dictionary<string, ProjectFile>();
        }
        
        public void AddNewfile(ProjectFile fileUploaded)
        {
            changedFileList.Add(fileUploaded);
        }

        public void ClearAll()
        {
            changedFileListDict.Clear();
            changedFileListQueue.Clear(); 
            changedFileList.Clear();
        }
    }
}