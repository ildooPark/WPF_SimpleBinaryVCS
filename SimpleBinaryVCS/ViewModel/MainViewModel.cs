using SimpleBinaryVCS.DataComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private VCSViewModel vcsVM;
        private UploaderViewModel uploaderVM;
        private BackupViewModel backupVM;
        public VCSViewModel VcsVM => vcsVM;
        public UploaderViewModel UploaderVM => uploaderVM;
        public BackupViewModel BackupVM => backupVM; 
        public MainViewModel()
        {
            vcsVM = new VCSViewModel();
            uploaderVM = new UploaderViewModel();
            backupVM = new BackupViewModel();
        }
    }
}
