using MemoryPack;
using SimpleBinaryVCS.Interfaces;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ProjectData : IEquatable<ProjectData>, IComparer<ProjectData>, IComparable<ProjectData>
    {
        public string projectName { get; set; }
        public string projectPath { get; set; }
        public string updaterName { get; set; }
        public DateTime updatedTime { get; set; }
        public string updatedVersion { get; set; }
        public int revisionNumber { get; set; }
        public string updateLog { get; set; }
        public string changeLog { get; set; }
        public int numberOfChanges { get; set; }
        private ObservableCollection<ProjectFile> projectFiles; 
        public ObservableCollection<ProjectFile> ProjectFiles 
        { 
            get => projectFiles ??= new ObservableCollection<ProjectFile>();
            set => projectFiles = value; 
        }
        private ObservableCollection<ProjectFile> diffLog;
        public ObservableCollection<ProjectFile> DiffLog
        {
            get => diffLog ??= new ObservableCollection<ProjectFile>();
            set => diffLog = value;
        }
        

        [MemoryPackConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProjectData()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        { 
        }

        public ProjectData(ProjectData srcProjectData, bool isRevert = false)
        {
            this.projectName = srcProjectData.projectName;
            this.projectPath = srcProjectData.projectPath;
            this.updaterName = srcProjectData.updaterName;
            this.updatedTime = srcProjectData.updatedTime;
            this.updatedVersion = srcProjectData.updatedVersion;
            this.revisionNumber = srcProjectData.revisionNumber;
            this.updateLog = srcProjectData.updateLog;
            this.changeLog = srcProjectData.changeLog;
            this.numberOfChanges = srcProjectData.numberOfChanges;
            this.projectFiles = new ObservableCollection<ProjectFile>();
            if (!isRevert)
            {
                this.diffLog = new ObservableCollection<ProjectFile>();
            }
            else
            {
                this.diffLog = new ObservableCollection<ProjectFile>(srcProjectData.DiffLog);
            }
        }

        public bool Equals(ProjectData? other)
        {
            if (other == null) return false;
            return other.updatedVersion == this.updatedVersion; 
        }

        public int Compare(ProjectData? x, ProjectData? y)
        {
            if (x == null || y == null)
            {
                MessageBox.Show("Invalid Comparison, ProjectData Cannot be Null");
                return 0;
            }
            if (x.revisionNumber.CompareTo(y.revisionNumber) == 0) 
                return x.updatedTime.CompareTo(y.updatedTime);
            return x.revisionNumber.CompareTo(y.revisionNumber);
        }

        public int CompareTo(ProjectData? other)
        {
            if (other == null)
            {
                MessageBox.Show("Invalid Comparison, ProjectData Cannot be Null");
                return 0;
            }
            if (this.revisionNumber.CompareTo(other.revisionNumber) == 0)
                return this.updatedTime.CompareTo(other.updatedTime);
            if (this.revisionNumber > other.revisionNumber) return -1;
            return 1; 
        }

        public void RegisterProjectInfo(Dictionary<string, object> dict)
        {
            dict.Add(nameof(this.projectName), this.projectName);
            dict.Add(nameof(this.projectPath), this.projectPath);
            dict.Add(nameof(this.updaterName), this.updaterName);
            dict.Add(nameof(this.updatedTime), this.updatedTime);
            dict.Add(nameof(this.updatedVersion), this.updatedVersion);
            dict.Add(nameof(this.revisionNumber), this.revisionNumber);
            dict.Add(nameof(this.numberOfChanges), this.numberOfChanges);
        }

        public List<string> GetProjectDirPaths()
        {
            LinkedList<ProjectData> nodeList = new LinkedList<ProjectData>(); 
            List<string> dirPaths = new List<string>();
            foreach (ProjectFile file in ProjectFiles)
            {

                if (Path.GetDirectoryName(file.fileRelPath) == null || 
                    Path.GetDirectoryName(file.fileRelPath) == string.Empty)
                    continue;
                dirPaths.Add(Path.GetDirectoryName(file.fileRelPath));
            }
            return dirPaths;
        }
        public List<string> GetProjectFileRelPaths()
        {
            List<string> filePaths = new List<string>();
            foreach (ProjectFile file in ProjectFiles)
            {
                filePaths.Add(file.fileRelPath);
            }
            return filePaths;
        }
    }
}