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
        private string changeLog; 
        public string ChangeLog
        {
            get { return changeLog ??= ""; }
            set
            {
                changeLog = value;
                OnPropertyChanged("ChangeLog");
            }
        }
        private string updateLog;
        public string UpdateLog
        {
            get { return updateLog ??= ""; }
            set
            {
                updateLog = value;
                OnPropertyChanged("UpdateLog");
            }
        }

        private ObservableCollection<ProjectFile> fileList;
        public ObservableCollection<ProjectFile> FileList
        {
            get=> fileList ??= new ObservableCollection<ProjectFile>();
            set
            {
                fileList = value;
                OnPropertyChanged("FileList");
            }
        }
        private Dictionary<string, object>? _projectDataDetail;
        public Dictionary<string, object> ProjectDataDetail
        {
            get => _projectDataDetail ??= new Dictionary<string, object>(); 
            set
            {
                _projectDataDetail = value;
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
            _projectDataDetail = new Dictionary<string, object>();
            projectData.RegisterProjectToDict(ProjectDataDetail);
            this.fileList = projectData.ProjectFiles;
            this.changeLog = projectData.changeLog ?? "Undefined";
            this.updateLog = projectData.updateLog ?? "Undefined";
        }
    }
}