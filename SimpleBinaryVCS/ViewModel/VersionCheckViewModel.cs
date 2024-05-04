﻿using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SimpleBinaryVCS.ViewModel
{
    public class VersionCheckViewModel : ViewModelBase
    {
        private string? changeLog; 
        public string ChangeLog
        {
            get { return changeLog ??= ""; }
            set
            {
                changeLog = value;
                OnPropertyChanged("ChangeLog");
            }
        }
        private string? updateLog;
        public string UpdateLog
        {
            get { return updateLog ??= ""; }
            set
            {
                updateLog = value;
                OnPropertyChanged("UpdateLog");
            }
        }

        private ICommand? similaritiesWithLocal;
        public ICommand? SimilaritiesWithLocal => similaritiesWithLocal ??= new RelayCommand(GetSimilarities);

        private void GetSimilarities(object? obj)
        {
            _metaDataManager.RequestProjectCompatibility(_projectData); 
        }

        private ICommand? conductIntegrate;
        public ICommand? ConductIntegrate => conductIntegrate ??= new RelayCommand(IntegrateToLocal);

        private void IntegrateToLocal(object? obj)
        {
            _metaDataManager.RequestProjectIntegrate(null, null, null); 
        }

        private ICommand? exportToXLSX;
        public ICommand ExportToXLSX => exportToXLSX ??= new RelayCommand(ExportXLSX, CanExportXLSX);

        private void ExportXLSX(object obj)
        {
            _metaDataManager.RequestExportProjectFilesXLSX(FileList, _projectData); 
        }

        private bool CanExportXLSX(object obj)
        {
            return FileList.Count > 0; 
        }

        private ObservableCollection<ProjectFile>? fileList;
        public ObservableCollection<ProjectFile> FileList
        {
            get=> fileList ??= new ObservableCollection<ProjectFile>();
            set
            {
                fileList = value;
                OnPropertyChanged("FileList");
            }
        }
        private Dictionary<string, object>? _projectDataReview;
        public Dictionary<string, object> ProjectDataReview
        {
            get => _projectDataReview ??= new Dictionary<string, object>(); 
            set
            {
                _projectDataReview = value;
                OnPropertyChanged("ProjectDataDetail");
            }
        }

        private MetaDataManager _metaDataManager;
        private readonly ProjectData _projectData; 
        public VersionCheckViewModel(ProjectData projectData, string versionLog, ObservableCollection<ProjectFile> fileList)
        {
            _metaDataManager = App.MetaDataManager;
            _projectData = projectData;
            this.updateLog = "Integrity Checking";
            this.changeLog = versionLog;
            this.fileList = fileList;
        }

        public VersionCheckViewModel(ProjectData projectData)
        {
            _metaDataManager = App.MetaDataManager;

            _projectData = projectData;
            _projectDataReview = new Dictionary<string, object>();
            _projectData.RegisterProjectInfo(ProjectDataReview);
            this.FileList = _projectData.ProjectFilesObs;
            this.ChangeLog = _projectData.ChangeLog ?? "Undefined";
            this.UpdateLog = _projectData.UpdateLog ?? "Undefined";
        }
    }
}