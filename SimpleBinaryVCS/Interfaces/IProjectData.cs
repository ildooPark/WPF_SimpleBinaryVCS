using SimpleBinaryVCS.DataComponent;

namespace SimpleBinaryVCS.Interfaces
{
    public enum ProjectDataType
    {
        File,
        Directory
    }
    public interface IProjectData
    {
        ProjectDataType DataType { get; }
        public DataChangedState DataState { get; set; }
        public DateTime UpdatedTime { get; set; }
        public string DataName { get; }
        public string DataRelPath { get; }
        public string DataSrcPath { get; }
        public string DataAbsPath { get; }
        public string DataHash { get; set; }
    }
}