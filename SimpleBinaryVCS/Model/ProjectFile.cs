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
        public bool IsNew { get; set; }
        public ProjectDataType DataType { get; private set; } = ProjectDataType.File; 
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
        #endregion
        #region [MemoryPackIgnore]
        [MemoryPackIgnore]
        public DataChangedState DataState { get => dataState; set => dataState = value; }
        [MemoryPackIgnore]
        public string DataName => dataName;
        [MemoryPackIgnore]
        public string DataSrcPath => dataSrcPath;

        [MemoryPackIgnore]
        public string DataRelPath => dataRelPath;
        [MemoryPackIgnore]
        public string DataHash { get => dataHash; set => dataHash = value; }
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
        public ProjectFile(bool isNew, long fileSize, string? fileVersion, string fileName, string fileSrcPath, string fileRelPath, DataChangedState changedState)
        {
            this.IsNew = isNew;
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

        public ProjectFile(bool IsNew, long FileSize, string FileName, string? FileBuildVersion, string FileSrcPath, string FileRelPath, string FileHash, string DeployedProjectVersion, DateTime updatedTime, DataChangedState fileState)
        {
            this.IsNew = IsNew;
            this.DataSize = FileSize;
            this.dataName = FileName;
            this.BuildVersion = FileBuildVersion ?? "";
            this.dataSrcPath = FileSrcPath;
            this.dataRelPath = FileRelPath;
            this.dataHash = FileHash;
            this.DeployedProjectVersion = DeployedProjectVersion;
            this.UpdatedTime = updatedTime; 
            this.dataState = fileState;
        }

        /// <summary>
        /// Deep Copy of ProjectFile
        /// </summary>
        /// <param name="srcData">Copying File</param>
        public ProjectFile(ProjectFile srcData)
        {
            this.IsNew = srcData.IsNew;
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

        public ProjectFile(string fileSrcPath, string fileRelPath, string fileHash, DataChangedState state)
        {
            this.IsNew = true;
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
        /// using ChangedFile Class, converts to ProjectFile, Sets isNew to true
        /// </summary>
        /// <param name="changedFile"></param>
        public ProjectFile(TrackedData changedFile, DataChangedState fileChangedState)
        {
            this.IsNew = true;
            this.dataState = fileChangedState; 
            var fileInfo = FileVersionInfo.GetVersionInfo(changedFile.DataAbsPath);
            this.DataSize = new FileInfo(changedFile.DataAbsPath).Length;
            this.BuildVersion = fileInfo.FileVersion ?? "";
            this.DeployedProjectVersion = "";
            this.dataSrcPath = changedFile.DataSrcPath;
            this.dataName = changedFile.DataName;
            this.dataRelPath = changedFile.DataRelPath;
            this.dataHash = changedFile.DataHash;
            this.UpdatedTime = changedFile.ChangedTime;
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
                MessageBox.Show($"Presented ProjectFile is Null for comparision with {this.N}"); 
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