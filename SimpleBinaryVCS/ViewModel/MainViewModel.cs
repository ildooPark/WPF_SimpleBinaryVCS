using SimpleBinaryVCS.Interfaces;

namespace SimpleBinaryVCS.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private MetaDataViewModel _metaDataVM;
        private FileTrackViewModel _fileTrackVM;
        private BackupViewModel _backupVM;
        public MetaDataViewModel MetaDataVM => _metaDataVM;
        public FileTrackViewModel FileTrackVM => _fileTrackVM;
        public BackupViewModel BackupVM => _backupVM; 
        public MainViewModel()
        {
            _metaDataVM = new MetaDataViewModel();
            _fileTrackVM = new FileTrackViewModel();
            _backupVM = new BackupViewModel();
            App.AwakeManagers();
        }
    }
}
