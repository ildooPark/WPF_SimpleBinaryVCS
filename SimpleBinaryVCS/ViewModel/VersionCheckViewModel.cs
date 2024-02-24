using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public VersionCheckViewModel(string versionLog, ObservableCollection<ProjectFile> fileList)
        {
            this.updateLog = "Integrity Checking";
            this.changeLog = versionLog;
            this.fileList = fileList;
        }

        public VersionCheckViewModel(ProjectData projectData)
        {
            _projectDataReview = new Dictionary<string, object>();
            projectData.RegisterProjectInfo(ProjectDataReview);
            this.FileList = projectData.ProjectFilesObs;
            this.ChangeLog = projectData.ChangeLog ?? "Undefined";
            this.UpdateLog = projectData.UpdateLog ?? "Undefined";
        }
    }
}