using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SimpleBinaryVCS.ViewModel
{
    public class OverlapFileViewModel : ViewModelBase
    {
        private List<ChangedFile>? overlapFilesList;
        public List<ChangedFile>? OverlapFilesList
        {
            get => overlapFilesList ??= new List<ChangedFile>();
            set
            {
                overlapFilesList = value;
                OnPropertyChanged("OverlapFilesList");
            }
        }

        private ICommand? confirmCommand;
        public ICommand ConfirmCommand => confirmCommand ??= new RelayCommand(ConfirmChoices);

        public event Action? ConfirmEventHandler;

        private void ConfirmChoices(object? obj)
        {
            throw new NotImplementedException();
            ConfirmEventHandler?.Invoke();
        }

        private MetaDataManager metaDataManager; 
        public OverlapFileViewModel(List<ChangedFile> registeredOverlaps)
        {
            this.overlapFilesList = registeredOverlaps;
            metaDataManager = App.MetaDataManager; 
        }


    }
}
