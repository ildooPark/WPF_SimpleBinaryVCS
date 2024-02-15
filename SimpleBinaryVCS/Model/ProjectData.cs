using MemoryPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ProjectData : IEquatable<ProjectData>, IComparer<ProjectData>, IComparable<ProjectData>
    {
        public string projectName { get; set; }
        public string projectPath { get; set; }
        public string? updaterName {  get; set; }
        public DateTime updatedTime { get; set; }
        public string? updatedVersion {  get; set; }
        public int revisionNumber { get; set; }
        public string? updateLog { get; set; }
        public string? changeLog { get; set; }
        public int numberOfChanges {  get; set; }
        private ObservableCollection<ProjectFile>? projectFiles; 
        public ObservableCollection<ProjectFile> ProjectFiles 
        { 
            get
            {
                if (projectFiles == null)
                {
                    projectFiles = new ObservableCollection<ProjectFile>();
                    return projectFiles;
                }
                else 
                    return projectFiles;
            }
            set
            { 
                projectFiles = value; 
            }
        }
        private ObservableCollection<ProjectFile>? diffLog;
        public ObservableCollection<ProjectFile> DiffLog
        {
            get
            {
                if (diffLog == null)
                {
                    diffLog = new ObservableCollection<ProjectFile>();
                    return diffLog;
                }
                else
                    return diffLog;
            }
            set
            {
                diffLog = value;
            }
        }
        private List<ProjectData> projectDataList;
        public List<ProjectData> ProjectDataList 
        { 
            get => projectDataList ??= (projectDataList = new List<ProjectData>());
            set
            {
                projectDataList = value;
            }
        }

        [MemoryPackConstructor]
        public ProjectData() 
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
            this.ProjectFiles = new ObservableCollection<ProjectFile>();

            if (!isRevert)
            {
                this.DiffLog = new ObservableCollection<ProjectFile>();
            }
            else
            {
                this.DiffLog = new ObservableCollection<ProjectFile>(srcProjectData.DiffLog);
            }
        }

        public bool Equals(ProjectData? other)
        {
            if (other == null) return false;
            return other.updatedVersion == this.updatedVersion; 
        }

        public int Compare(ProjectData? x, ProjectData? y)
        {
            if (x.revisionNumber.CompareTo(y.revisionNumber) == 0) 
                return x.updatedTime.CompareTo(y.updatedTime);
            return x.revisionNumber.CompareTo(y.revisionNumber);
        }

        public int CompareTo(ProjectData? other)
        {
            if (this.revisionNumber.CompareTo(other.revisionNumber) == 0)
                return this.updatedTime.CompareTo(other.updatedTime);
            if (this.revisionNumber > other.revisionNumber) return -1;
            return 1; 
        }

        public void RegisterProjectToDict(Dictionary<string, object> dict)
        {
            dict.Add(nameof(this.projectName), this.projectName);
            dict.Add(nameof(this.projectPath), this.projectPath);
            dict.Add(nameof(this.updaterName), this.updaterName);
            dict.Add(nameof(this.updatedTime), this.updatedTime);
            dict.Add(nameof(this.updatedVersion), this.updatedVersion);
            dict.Add(nameof(this.revisionNumber), this.revisionNumber);
            dict.Add(nameof(this.numberOfChanges), this.numberOfChanges);
        }
    }
}