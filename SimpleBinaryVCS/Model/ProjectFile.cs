﻿using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;

namespace SimpleBinaryVCS.Model
{
    public class ProjectFile : IEquatable<ProjectFile>, IComparable<ProjectFile>, IProjectData
    {
        #region Serialize constructor variables
        public ProjectDataType DataType { get; private set; }
        public long DataSize { get; set; }
        public string BuildVersion {  get; set; }
        public string DeployedProjectVersion { get; set; }
        public DateTime UpdatedTime { get; set; }
        public bool IsDstFile { get; set; }
        #endregion
        public DataState DataState { get; set; }
        public string DataName { get; set; }
        public string DataSrcPath { get; set; }
        public string DataRelPath { get; set; }
        public string DataHash { get; set; }
        #region Json Constructor ignored. 

        [JsonIgnore] 
        public string DataAbsPath => Path.Combine(DataSrcPath, DataRelPath);
        #endregion
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProjectFile() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [JsonConstructor]
        public ProjectFile(ProjectDataType DataType, long DataSize, string BuildVersion, string DeployedProjectVersion, 
            DateTime UpdatedTime, DataState DataState, string dataName, string dataSrcPath, string dataRelPath, string dataHash, bool IsDstFile) 
        {
            this.DataType = DataType;
            this.DataSize = DataSize;
            this.BuildVersion = BuildVersion;
            this.DeployedProjectVersion = DeployedProjectVersion;
            this.UpdatedTime = UpdatedTime;
            this.DataState = DataState;
            this.DataName = dataName;
            this.DataSrcPath = dataSrcPath;
            this.DataRelPath = dataRelPath;
            this.DataHash = dataHash;
            this.IsDstFile = IsDstFile;
        }
        #region Constructors
        /// <summary>
        /// Lacks FileHash, DeployedProjectVersion, FileChangedState
        /// </summary>
        public ProjectFile(long fileSize, string? fileVersion, string fileName, string fileSrcPath, string fileRelPath, DataState changedState)
        {
            this.DataSize = fileSize;
            this.BuildVersion = fileVersion ?? "";
            this.DataName = fileName;
            this.DataSrcPath = fileSrcPath;
            this.DataRelPath = fileRelPath;
            this.DataHash = "";
            this.DeployedProjectVersion = "";
            this.UpdatedTime = DateTime.Now;
            this.DataState = changedState;
        }
        /// <summary>
        /// For PreStagedProjectFile Data Type = File 
        /// </summary>
        public ProjectFile(long DataSize, string? BuildVersion, string DataName, string DataSrcPath, string DataRelPath)
        {
            this.DataType = ProjectDataType.File;
            this.DataSize = DataSize;
            this.BuildVersion = BuildVersion ?? "";
            this.DeployedProjectVersion = "";
            this.UpdatedTime = DateTime.MinValue;
            this.DataState = DataState.PreStaged;
            this.DataName = DataName;
            this.DataSrcPath = DataSrcPath;
            this.DataRelPath= DataRelPath;
            this.DataHash = "";
        }
        /// <summary>
        /// For PreStagedProjectFile Data Type = Directory 
        /// </summary>
        public ProjectFile(string DataName, string DataSrcPath, string DataRelPath)
        {
            this.DataType = ProjectDataType.Directory;
            this.DataSize = 0;
            this.BuildVersion = "";
            this.DeployedProjectVersion = "";
            this.UpdatedTime = DateTime.MinValue;
            this.DataState = DataState.PreStaged;
            this.DataName = DataName;
            this.DataSrcPath = DataSrcPath;
            this.DataRelPath = DataRelPath;
            this.DataHash = "";
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
            this.DataState = srcData.DataState;
            this.DataName = srcData.DataName;
            this.DataSrcPath = srcData.DataSrcPath;
            this.DataRelPath = srcData.DataRelPath;
            this.DataHash = srcData.DataHash;
        }
        public ProjectFile(ProjectFile updatedData, string deployedProjectVersion)
        {
            this.DataType = updatedData.DataType;
            this.DataSize = updatedData.DataSize;
            this.BuildVersion = updatedData.BuildVersion;
            this.DeployedProjectVersion = deployedProjectVersion;
            this.UpdatedTime = DateTime.Now;
            this.DataState = updatedData.DataState;
            this.DataName = updatedData.DataName;
            this.DataSrcPath = updatedData.DataSrcPath;
            this.DataRelPath = updatedData.DataRelPath;
            this.DataHash = updatedData.DataHash;
        }
        public ProjectFile(ProjectFile srcData, DataState state)
        {
            this.DataType = srcData.DataType;
            this.DataSize = srcData.DataSize;
            this.BuildVersion = srcData.BuildVersion;
            this.DeployedProjectVersion = srcData.DeployedProjectVersion;
            this.UpdatedTime = DateTime.Now;
            this.DataState = state;
            this.DataName = srcData.DataName;
            this.DataSrcPath = srcData.DataSrcPath;
            this.DataRelPath = srcData.DataRelPath;
            this.DataHash = srcData.DataHash;
        }
        public ProjectFile(ProjectFile srcData, DataState DataState, string dataSrcPath)
        {
            this.DataType = srcData.DataType;
            this.DataSize = srcData.DataSize;
            this.BuildVersion = srcData.BuildVersion;
            this.DeployedProjectVersion = srcData.DeployedProjectVersion;
            this.UpdatedTime = DateTime.Now;
            this.DataState = DataState;
            this.DataName = srcData.DataName;
            this.DataSrcPath = dataSrcPath;
            this.DataRelPath = srcData.DataRelPath;
            this.DataHash = srcData.DataHash;
        }
        public ProjectFile(string fileSrcPath, string fileRelPath, string? fileHash, DataState DataState, ProjectDataType dataType)
        {
            string fileFullPath = Path.Combine(fileSrcPath, fileRelPath);
            var fileInfo = FileVersionInfo.GetVersionInfo(fileFullPath);
            this.DataSize = new FileInfo(fileFullPath).Length; 
            this.BuildVersion = fileInfo.FileVersion ?? "";
            this.DeployedProjectVersion = "";
            this.DataSrcPath = fileSrcPath; 
            this.DataName = Path.GetFileName(fileFullPath);
            this.DataRelPath = fileRelPath;
            this.DataHash = fileHash ?? "";
            this.UpdatedTime = DateTime.Now;
            this.DataState = DataState;
            this.DataType = dataType;
        }
        #endregion
        public int CompareTo(ProjectFile? other) 
        {
            if (this.UpdatedTime.CompareTo(other.UpdatedTime) == 0)
                return this.DataSize.CompareTo(other.DataSize);
            return this.UpdatedTime.CompareTo(other.UpdatedTime);
        }
        /// <summary>
        /// Checks fileName
        /// </summary>
        public bool Equals(ProjectFile? other)
        {
            if (other == null)
            {
                MessageBox.Show($"Presented ProjectFile is Null for comparision with {this.DataName}"); 
                return false;
            }
            return other.DataName == this.DataName;
        }
        /// <summary>
        /// Returns False if not Same
        /// </summary>
        public bool CheckSize(ProjectFile other)
        {
            return other.DataSize == this.DataSize;
        }
    }
}