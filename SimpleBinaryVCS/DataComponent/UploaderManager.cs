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
        public ObservableCollection<FileBase> g_uploadedFileList;

        public UploaderManager()
        {
            g_uploadedFileList = new ObservableCollection<FileBase>();
        }

        public void AddNewfile(FileBase fileUploaded)
        {
            g_uploadedFileList.Add(fileUploaded);
        }

        public void RemoveAll()
        {
            g_uploadedFileList.Clear();
        }
    }
}