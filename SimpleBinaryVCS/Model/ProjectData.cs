using MemoryPack;
using System.Collections.ObjectModel;
using System.IO;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ProjectData : IEquatable<ProjectData>, IComparer<ProjectData>, IComparable<ProjectData>
    {
        public string ProjectName { get; set; }
        public string ProjectPath { get; set; }
        public string UpdaterName { get; set; }
        public string ConductedPC { get; set; }
        public DateTime UpdatedTime { get; set; }
        public string UpdatedVersion { get; set; }
        public string UpdateLog { get; set; }
        public string ChangeLog { get; set; }
        public int NumberOfChanges { get; set; }
        private ObservableCollection<ProjectFile> projectFiles; 
        public ObservableCollection<ProjectFile> ProjectFiles 
        { 
            get => projectFiles ??= new ObservableCollection<ProjectFile>();
            set => projectFiles = value; 
        }
        private ObservableCollection<ProjectFile> changedFiles;
        public ObservableCollection<ProjectFile> ChangedFiles
        {
            get => changedFiles ??= new ObservableCollection<ProjectFile>();
            set => changedFiles = value;
        }
        

        [MemoryPackConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProjectData()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        { 
        }

        public ProjectData(ProjectData srcProjectData, bool isRevert = false)
        {
            this.ProjectName = srcProjectData.ProjectName;
            this.ProjectPath = srcProjectData.ProjectPath;
            this.UpdaterName = srcProjectData.UpdaterName;
            this.UpdatedTime = srcProjectData.UpdatedTime;
            this.UpdatedVersion = srcProjectData.UpdatedVersion;
            this.ConductedPC = srcProjectData.ConductedPC;
            this.UpdateLog = srcProjectData.UpdateLog;
            this.ChangeLog = srcProjectData.ChangeLog;
            this.NumberOfChanges = srcProjectData.NumberOfChanges;
            this.projectFiles = new ObservableCollection<ProjectFile>();
            if (!isRevert)
            {
                this.changedFiles = new ObservableCollection<ProjectFile>();
            }
            else
            {
                this.changedFiles = new ObservableCollection<ProjectFile>(srcProjectData.ChangedFiles);
            }
        }

        public bool Equals(ProjectData? other)
        {
            if (other == null) return false;
            return other.UpdatedVersion == this.UpdatedVersion; 
        }

        public int Compare(ProjectData? x, ProjectData? y)
        {
            if (x == null || y == null)
            {
                MessageBox.Show("Invalid Comparison, ProjectData Cannot be Null");
                return 0;
            }
            return x.UpdatedTime.CompareTo(y.UpdatedTime);
        }

        public int CompareTo(ProjectData? other)
        {
            if (other == null)
            {
                MessageBox.Show("Invalid Comparison, ProjectData Cannot be Null");
                return 0;
            }
            return this.UpdatedTime.CompareTo(other.UpdatedTime);
        }

        public void RegisterProjectInfo(Dictionary<string, object> dict)
        {
            dict.Add(nameof(this.ProjectName), this.ProjectName);
            dict.Add(nameof(this.ProjectPath), this.ProjectPath);
            dict.Add(nameof(this.UpdaterName), this.UpdaterName);
            dict.Add(nameof(this.ConductedPC), this.ConductedPC);
            dict.Add(nameof(this.UpdatedTime), this.UpdatedTime);
            dict.Add(nameof(this.UpdatedVersion), this.UpdatedVersion);
            dict.Add(nameof(this.NumberOfChanges), this.NumberOfChanges);
        }

        public List<string> ProjectRelDirs()
        {
            List<string> dirPaths = new List<string>();
            foreach (ProjectFile file in ProjectFiles)
            {
                string? dirPath = Path.GetDirectoryName(file.DataRelPath); 
                if (dirPath == null ||
                    dirPath == string.Empty)
                    continue;
                dirPaths.Add(dirPath);
            }
            return dirPaths;
        }

        public List<string> ProjectFileRelPaths()
        {
            List<string> filePaths = new List<string>();
            foreach (ProjectFile file in ProjectFiles)
            {
                filePaths.Add(file.DataRelPath);
            }
            return filePaths;
        }
    }
}