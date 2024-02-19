using MemoryPack;
using SimpleBinaryVCS.Interfaces;
using System.Collections.ObjectModel;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ProjectRepository
    {
        public int RevisionNumber { get; set; }
        public string ProjectName {  get; set; }
        public string ProjectPath { get; set; }
        private ProjectData projectMain;
        public ProjectData ProjectMain { get; set; }
        [MemoryPackInclude]
        private LinkedList<ProjectData>? projectDataList;
        [MemoryPackIgnore]
        public LinkedList<ProjectData> ProjectDataList
        {
            get => projectDataList ??= (projectDataList = new LinkedList<ProjectData>());
            set => projectDataList = value;
        }
        [MemoryPackInclude]
        private Dictionary<string, IProjectData> backupFiles;

        [MemoryPackIgnore]
        public Dictionary<string, IProjectData> BackupFiles 
        {
            get => backupFiles ??= new Dictionary<string, IProjectData>(); 
            set => backupFiles = value;
        }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProjectRepository() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    
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
