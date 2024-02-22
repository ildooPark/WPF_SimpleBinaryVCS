using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WinForms = System.Windows.Forms;

namespace SimpleBinaryVCS.ViewModel
{
    public class MetaDataViewModel : ViewModelBase
    {
        private ProjectData? projectData; 
        public ProjectData? ProjectData
        {
            get => projectData ?? null;
            set
            {
                projectData = value;
                ProjectFiles = value?.ProjectFiles;
                ProjectName = value?.ProjectName ?? "Undefined";
                CurrentVersion = value?.UpdatedVersion ?? "Undefined"; 
            }
        }

        private ObservableCollection<ProjectFile>? projectFiles;
        public ObservableCollection<ProjectFile>? ProjectFiles
        {
            get => projectFiles;
            set
            {
                projectFiles = value;
                OnPropertyChanged("ProjectFiles");
            }
        }

        private string? updaterName;
        public string? UpdaterName
        {
            get => updaterName ?? "";
            set
            {
                updaterName = value;
                OnPropertyChanged("UpdaterName");
            }
        }

        private string? updateLog;
        public string? UpdateLog
        {
            get => updateLog ?? "";
            set
            {
                updateLog = value;
                OnPropertyChanged("UpdateLog");
            }
        }

        private string? projectName;
        public string ProjectName
        {
            get => projectName ?? "Undefined";
            set
            {
                projectName = value ?? "Undefined";
                OnPropertyChanged("ProjectName");
            }
        }

        private string? currentVersion;
        public string CurrentVersion
        {
            get => currentVersion ?? "Undefined";
            set
            {
                currentVersion = value ?? "Undefined";
                OnPropertyChanged("CurrentVersion");
            }
        }

        private ICommand? conductUpdate;
        public ICommand ConductUpdate
        {
            get
            {
                if (conductUpdate == null) conductUpdate = new RelayCommand(Update, CanUpdate);
                return conductUpdate; 
            }
        }

        private ICommand? getProject;
        public ICommand GetProject
        {
            get
            {
                if (getProject == null) getProject = new RelayCommand(RetrieveProject, CanRetrieveProject);
                return getProject;
            }
        }

        private MetaDataManager metaDataManager;
        private FileManager fileManager; 
        private BackupManager backupManager;
        private UpdateManager updateManager;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MetaDataViewModel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            metaDataManager = App.MetaDataManager;
            fileManager = App.FileManager;
            backupManager = App.BackupManager;
            updateManager = App.UpdateManager;

            metaDataManager.ResetAction += ProjectLoadResponse;
            metaDataManager.ProjectLoaded += ProjectLoadResponse;
            updateManager.UpdateAction += UpdateResponse;
            backupManager.RevertAction += RevertResponse;
        }

        #region Update Version 
        private bool CanUpdate(object obj)
        {
            if (projectFiles == null || fileManager.ChangedFileList.Count == 0) return false;
            if (ProjectData?.ProjectPath == null || updaterName == "" || updateLog == "") return false;
            return true;
        }
        
        private void Update(object obj)
        {
            if (updaterName == null || updateLog == null || updaterName == "" || updateLog == "")
            {
                var response = MessageBox.Show("Must Have both Deploy Version AND UpdaterName", "ok", MessageBoxButtons.OK);
                if (response == DialogResult.OK) return;
                return;
            }
            if (fileManager.ChangedFileList.Count == 0 || fileManager == null) return;
            updateManager.UpdateProjectMain();
        }

        private bool CanRetrieveProject(object parameter)
        {
            return true;
        }
        private void RetrieveProject(object parameter)
        {
            if (projectFiles != null && projectFiles.Count != 0) projectFiles.Clear();
            var openFD = new WinForms.FolderBrowserDialog();
            string? projectPath;
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                projectPath = openFD.SelectedPath;
            }
            else return;
            openFD.Dispose();
            if (string.IsNullOrEmpty(projectPath)) return;

            bool retrieveProjectResult = metaDataManager.TryRetrieveProject(projectPath);
            if (!retrieveProjectResult)
            {
                var result = MessageBox.Show("VersionLog file not found!\n Initialize A New Project?",
                    "Import Project", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    metaDataManager.InitializeProject(openFD.SelectedPath);
                }
                else
                {
                    MessageBox.Show("Please Select Another Project Path");
                    return;
                }
            }
        }
        #endregion
        #region Receiving Model Callbacks
        private void VersionIntegrityCheck(object projObj)
        {
            // After Revert Changes, 
            // Any Detected Changes should be enlisted to the FileManager.DetectedFileChanges for the Push
        }

        private void UpdateResponse(object projObj)
        {
            if (projObj is not ProjectData projectData) return;
            ProjectData = projectData;
            ProjectName = ProjectData.ProjectName ?? "Undefined";
            CurrentVersion = ProjectData.UpdatedVersion ?? "Undefined";
        }

        private void RevertResponse(object projObj)
        {
            if (projObj is not ProjectData projectData) return;
            this.ProjectData = projectData;
        }

        private void ProjectLoadResponse(object projObj)
        {
            if (projObj is not ProjectData projectData) return; 
            this.ProjectData = projectData;
        }
        #endregion

    }
}