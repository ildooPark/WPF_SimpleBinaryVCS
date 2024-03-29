﻿using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using SimpleBinaryVCS.View;
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
        public event Action TaskFinishedEventHandler; 
        private MetaDataManager metaDataManager;

        public OverlapFileViewModel(List<ChangedFile> registeredOverlaps)
        {
            this.overlapFilesList = registeredOverlaps;
            metaDataManager = App.MetaDataManager;
        }

        private void ConfirmChoices(object? obj)
        {
            metaDataManager.RequestOverlappedFileAllocation(overlapFilesList);
            TaskFinishedEventHandler?.Invoke();
        }
    }
}
