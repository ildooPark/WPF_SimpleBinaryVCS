using MemoryPack;
using SimpleBinaryVCS.Interfaces;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ProjectRepository
    {
        public int UpdateCount { get; set; }
        public string ProjectName {  get; set; }
        public string ProjectPath { get; set; }

        [MemoryPackInclude]
        private ProjectData? projectMain;
        [MemoryPackIgnore]
        public ProjectData ProjectMain 
        {
            get => projectMain ?? throw new ArgumentNullException(nameof(projectMain));
            set => projectMain = value;
        }

        [MemoryPackInclude]
        private LinkedList<ProjectData>? projectDataList;
        [MemoryPackIgnore]
        public LinkedList<ProjectData> ProjectDataList
        {
            get => projectDataList ?? throw new ArgumentNullException(nameof(projectMain));
            set => projectDataList = value;
        }

        [MemoryPackInclude]
        private Dictionary<string, IProjectData> backupFiles;
        [MemoryPackIgnore]
        public Dictionary<string, IProjectData> BackupFiles 
        {
            get => backupFiles ?? throw new ArgumentNullException(nameof(projectMain)); 
            set => backupFiles = value;
        }
        #region Constructor 
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [MemoryPackConstructor]
        public ProjectRepository() { }
        public ProjectRepository(string projectName, string projectPath)
        {
            this.ProjectName = projectName;
            this.ProjectPath = projectPath;
            this.projectMain = new ProjectData();
            this.projectDataList = new LinkedList<ProjectData>();
            this.backupFiles = new Dictionary<string, IProjectData>();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        #endregion
        public ObservableCollection<ProjectData> ObservableProjectList()
        {
            ObservableCollection<ProjectData> dataList = new ObservableCollection<ProjectData>();
            foreach (ProjectData pd in ProjectDataList)
            {
                dataList.Add(pd);
            }
            return dataList;
        }
    }
}