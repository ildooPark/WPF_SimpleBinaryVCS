using MemoryPack;
using SimpleBinaryVCS.Interfaces;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace SimpleBinaryVCS.Model
{
    public class ProjectMetaData
    {
        public int UpdateCount;
        public string ProjectName;
        public string ProjectPath;
        public ProjectData ProjectMain {  get; set; }
        public LinkedList<ProjectData> ProjectDataList {  get; set; }   
        public Dictionary<string, ProjectFile> BackupFiles { get; set; }
        #region Constructor 
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [JsonConstructor]
        public ProjectMetaData() { }

        public ProjectMetaData(string projectName, string projectPath)
        {
            this.UpdateCount = 0;
            this.ProjectName = projectName;
            this.ProjectPath = projectPath;
            this.ProjectMain = new ProjectData();
            this.ProjectDataList = new LinkedList<ProjectData>();
            this.BackupFiles = new Dictionary<string, ProjectFile>();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        #endregion
        
    }
}