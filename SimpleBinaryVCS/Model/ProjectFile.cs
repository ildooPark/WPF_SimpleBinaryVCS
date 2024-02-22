using MemoryPack;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using System.Diagnostics;
using System.IO;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ProjectFile : IEquatable<ProjectFile>, IProjectData
    {
        #region [MemoryPackInclude]
        public ProjectDataType DataType { get; private set; }
        public long DataSize { get; set; }
        public string BuildVersion {  get; set; }
        public string DeployedProjectVersion { get; set; }
        public DateTime UpdatedTime { get; set; }
        [MemoryPackInclude]
        private DataChangedState dataState;
        [MemoryPackInclude]
        private string dataName;
        [MemoryPackInclude]
        private string dataSrcPath;
        [MemoryPackInclude]
        private string dataRelPath;
        [MemoryPackInclude]
        private string dataHash;
        public bool IsDstFile { get; set; }
        #endregion
        #region [MemoryPackIgnore]
        [MemoryPackIgnore]
        public DataChangedState DataState { get => dataState; set => dataState = value; }
        [MemoryPackIgnore]
        public string DataName => dataName;
        [MemoryPackIgnore]
        public string DataSrcPath { get => dataSrcPath; set => dataSrcPath = value; }
        [MemoryPackIgnore]
        public string DataRelPath => dataRelPath;
        [MemoryPackIgnore]
        public string DataHash { get => dataHash ?? ""; set => dataHash = value; }
        [MemoryPackIgnore]
        public string DataAbsPath => Path.Combine(dataSrcPath, dataRelPath);
        #endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [MemoryPackConstructor]
        public ProjectFile() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        #region Constructors
        /// <summary>
        /// Lacks FileHash, DeployedProjectVersion, FileChangedState
        /// </summary>
        /// <param name="isNew"></param>
        /// <param name="fileSize"></param>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        /// <param name="fileVersion"></param>
        public ProjectFile(long fileSize, string? fileVersion, string fileName, string fileSrcPath, string fileRelPath, DataChangedState changedState)
        {
            this.DataSize = fileSize;
            this.BuildVersion = fileVersion ?? "";
            this.dataName = fileName;
            this.dataSrcPath = fileSrcPath;
            this.dataRelPath = fileRelPath;
            this.dataHash = "";
            this.DeployedProjectVersion = "";
            this.UpdatedTime = DateTime.Now;
            this.dataState = changedState;
        }

        public ProjectFile(ProjectDataType DataType, long DataSize, string? BuildVersion, string? DeployedProjectVersion, 
            DateTime? UpdateTime, DataChangedState DataState, string DataName, string DataSrcPath, string DataRelPath, string? DataHash)
        {
            this.DataType = DataType;
            this.DataSize = DataSize;
            this.BuildVersion = BuildVersion ?? "";
            this.DeployedProjectVersion = DeployedProjectVersion ?? "";
            this.UpdatedTime = UpdateTime ?? DateTime.MinValue;
            this.DataState = DataState;
            this.dataName = DataName;
            this.dataSrcPath = DataSrcPath;
            this.dataRelPath= DataRelPath;
            this.dataHash = dataHash ?? "";
        }

        /// <summary>
        /// Deep Copy of ProjectFile
        /// </summary>
        /// <param name="srcData">Copying File</param>
        public ProjectFile(ProjectFile srcData)
        {
            this.DataType = srcData.DataType;
            this.DataSize = srcData.DataSize;
            this.BuildVersion = srcData.BuildVersion;
            this.DeployedProjectVersion = srcData.DeployedProjectVersion;
            this.UpdatedTime = srcData.UpdatedTime;
            this.dataState = srcData.DataState;
            this.dataName = srcData.DataName;
            this.dataSrcPath = srcData.DataSrcPath;
            this.dataRelPath = srcData.DataRelPath;
            this.dataHash = srcData.DataHash;
        }

        public ProjectFile(ProjectFile srcData, DataChangedState state)
        {
            this.DataType = srcData.DataType;
            this.DataSize = srcData.DataSize;
            this.BuildVersion = srcData.BuildVersion;
            this.DeployedProjectVersion = srcData.DeployedProjectVersion;
            this.UpdatedTime = DateTime.Now;
            this.dataState = state;
            this.dataName = srcData.DataName;
            this.dataSrcPath = srcData.DataSrcPath;
            this.dataRelPath = srcData.DataRelPath;
            this.dataHash = srcData.DataHash;
        }

        public ProjectFile(ProjectFile srcData, DataChangedState state, string dataSrcPath)
        {
            this.DataType = srcData.DataType;
            this.DataSize = srcData.DataSize;
            this.BuildVersion = srcData.BuildVersion;
            this.DeployedProjectVersion = srcData.DeployedProjectVersion;
            this.UpdatedTime = DateTime.Now;
            this.dataState = state;
            this.dataName = srcData.DataName;
            this.dataSrcPath = dataSrcPath;
            this.dataRelPath = srcData.DataRelPath;
            this.dataHash = srcData.DataHash;
        }

        public ProjectFile(string fileSrcPath, string fileRelPath, string fileHash, DataChangedState state)
        {
            string fileFullPath = Path.Combine(fileSrcPath, fileRelPath);
            var fileInfo = FileVersionInfo.GetVersionInfo(fileFullPath);
            this.DataSize = new FileInfo(fileFullPath).Length; 
            this.BuildVersion = fileInfo.FileVersion ?? "";
            this.DeployedProjectVersion = "";
            this.dataSrcPath = fileSrcPath; 
            this.dataName = Path.GetFileName(fileFullPath);
            this.dataRelPath = fileRelPath;
            this.dataHash = fileHash;
            this.UpdatedTime = DateTime.Now;
            this.dataState = state;
        }

        /// <summary>
        /// First compares fileVersion, then the updatedTime; 
        /// smaller fileVersion corresponds to newer file
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        #endregion
        public int CompareTo(ProjectFile other) 
        {
            if (this.UpdatedTime.CompareTo(other.UpdatedTime) == 0)
                return this.DataSize.CompareTo(other.DataSize);
            return this.UpdatedTime.CompareTo(other.UpdatedTime);
        }
        
        /// <summary>
        /// Checks 1. fileName, 2. fileVersion 
        /// IF all returns as true, then MD5 checksum is used to compute the differences.
        /// </summary>
        /// <param name = "other" ></ param >
        /// < returns ></ returns >
        public bool Equals(ProjectFile? other)
        {
            if (other == null)
            {
                MessageBox.Show($"Presented ProjectFile is Null for comparision with {this.DataName}"); 
                return false;
            }
            //if (other?.fileRelPath == this.fileRelPath) 
            //    return other.fileHash == this.fileHash;
            return other.DataRelPath == this.DataRelPath;
        }
        
        /// <summary>
        /// Returns False if not Same
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CheckSize(ProjectFile other)
        {
            return other.DataSize == this.DataSize;
        }
    }
}