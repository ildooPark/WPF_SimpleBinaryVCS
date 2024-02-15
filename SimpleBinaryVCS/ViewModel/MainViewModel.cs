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
        private FileTrackViewModel fileTrackVM;
        private BackupViewModel backupVM;
        public VCSViewModel VcsVM => vcsVM;
        public FileTrackViewModel FileTrackVM => fileTrackVM;
        public BackupViewModel BackupVM => backupVM; 
        public MainViewModel()
        {
            vcsVM = new VCSViewModel();
            fileTrackVM = new FileTrackViewModel();
            backupVM = new BackupViewModel();
        }
    }
}
