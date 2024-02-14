using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.ViewModel
{
    public class VersionCheckViewModel : ViewModelBase
    {
        private string integrityCheckLog; 
        public string IntegrityCheckLog
        {
            get { return integrityCheckLog; }
            set
            {
                integrityCheckLog = value;
                OnPropertyChanged("IntegrityCheckLog");
            }
        }
        public VersionCheckViewModel() 
        {
            IntegrityCheckLog = App.FileManager.VersionCheckLog;
        }
    }
}
