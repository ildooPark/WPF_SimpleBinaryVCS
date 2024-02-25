﻿using MemoryPack;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;

namespace SimpleBinaryVCS.Model
{
    public class ProjectFile : IEquatable<ProjectFile>, IComparable<ProjectFile>, ICloneable, IProjectData
    {
        #region [MemoryPackInclude]
        public ProjectDataType DataType { get; private set; }
        public long DataSize { get; set; }
        public string BuildVersion {  get; set; }
        public string DeployedProjectVersion { get; set; }
        public DateTime UpdatedTime { get; set; }
        private DataState dataState;
        private string dataName;
        private string dataSrcPath;
        private string dataRelPath;
        private string dataHash;
        public bool IsDstFile { get; set; }
        #endregion
        #region [MemoryPackIgnore]
        public DataState DataState { get => dataState; set => dataState = value; }
        public string DataName => dataName;
        public string DataSrcPath { get => dataSrcPath; set => dataSrcPath = value; }
        public string DataRelPath => dataRelPath;
        public string DataHash { get => dataHash ?? ""; set => dataHash = value; }
        public string DataAbsPath => Path.Combine(dataSrcPath, dataRelPath);
        #endregion
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProjectFile() { }
        [JsonConstructor]
        public ProjectFile(ProjectDataType DataType, long DataSize, string BuildVersion, string DeployedProjectVersion, 
            DateTime UpdatedTime, DataState dataState, string dataName, string dataSrcPath, string dataRelPath, string dataHash, bool IsDstFile) 
        {
            this.DataType = DataType;
            this.DataSize = DataSize;
            this.BuildVersion = BuildVersion;
            this.DeployedProjectVersion = DeployedProjectVersion;
            this.UpdatedTime = UpdatedTime;
            this.DataState = dataState;
            this.dataName = dataName;
            this.dataSrcPath = dataSrcPath;
            this.dataRelPath = dataRelPath;
            this.dataHash = dataHash;
            this.IsDstFile = IsDstFile;
        }
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
        public ProjectFile(long fileSize, string? fileVersion, string fileName, string fileSrcPath, string fileRelPath, DataState changedState)
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
        /// <summary>
        /// For PreStagedProjectFile Data Type = File 
        /// </summary>
        /// <param name="DataType"></param>
        /// <param name="DataSize"></param>
        /// <param name="BuildVersion"></param>
        /// <param name="DeployedProjectVersion"></param>
        /// <param name="UpdateTime"></param>
        /// <param name="DataState"></param>
        /// <param name="DataName"></param>
        /// <param name="DataSrcPath"></param>
        /// <param name="DataRelPath"></param>
        /// <param name="DataHash"></param>
        public ProjectFile(long DataSize, string? BuildVersion, string DataName, string DataSrcPath, string DataRelPath)
        {
            this.DataType = ProjectDataType.File;
            this.DataSize = DataSize;
            this.BuildVersion = BuildVersion ?? "";
            this.DeployedProjectVersion = "";
            this.UpdatedTime = DateTime.MinValue;
            this.DataState = DataState.PreStaged;
            this.dataName = DataName;
            this.dataSrcPath = DataSrcPath;
            this.dataRelPath= DataRelPath;
            this.dataHash = "";
        }
        /// <summary>
        /// For PreStagedProjectFile Data Type = Directory 
        /// </summary>
        /// <param name="DataType"></param>
        /// <param name="DataSize"></param>
        /// <param name="BuildVersion"></param>
        /// <param name="DeployedProjectVersion"></param>
        /// <param name="UpdateTime"></param>
        /// <param name="DataState"></param>
        /// <param name="DataName"></param>
        /// <param name="DataSrcPath"></param>
        /// <param name="DataRelPath"></param>
        /// <param name="DataHash"></param>
        public ProjectFile(string DataName, string DataSrcPath, string DataRelPath)
        {
            this.DataType = ProjectDataType.Directory;
            this.DataSize = 0;
            this.BuildVersion = "";
            this.DeployedProjectVersion = "";
            this.UpdatedTime = DateTime.MinValue;
            this.DataState = DataState.PreStaged;
            this.dataName = DataName;
            this.dataSrcPath = DataSrcPath;
            this.dataRelPath = DataRelPath;
            this.dataHash = "";
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
        public ProjectFile(ProjectFile srcData, DataState state)
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
        public ProjectFile(ProjectFile srcData, DataState state, string dataSrcPath)
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
        public ProjectFile(string fileSrcPath, string fileRelPath, string? fileHash, DataState state, ProjectDataType dataType)
        {
            string fileFullPath = Path.Combine(fileSrcPath, fileRelPath);
            var fileInfo = FileVersionInfo.GetVersionInfo(fileFullPath);
            this.DataSize = new FileInfo(fileFullPath).Length; 
            this.BuildVersion = fileInfo.FileVersion ?? "";
            this.DeployedProjectVersion = "";
            this.dataSrcPath = fileSrcPath; 
            this.dataName = Path.GetFileName(fileFullPath);
            this.dataRelPath = fileRelPath;
            this.dataHash = fileHash ?? "";
            this.UpdatedTime = DateTime.Now;
            this.dataState = state;
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

        public object Clone()
        {
            ProjectFile clone = new ProjectFile();
            clone.DataType = this.DataType;
            clone.DataSize = this.DataSize;
            clone.BuildVersion = this.BuildVersion;
            clone.DeployedProjectVersion = this.DeployedProjectVersion;
            clone.UpdatedTime = this.UpdatedTime;
            clone.dataState = this.dataState;
            clone.dataName = this.dataName;
            clone.dataSrcPath = this.dataSrcPath;
            clone.dataRelPath = this.dataRelPath;
            clone.dataHash = this.dataHash;
            clone.IsDstFile = this.IsDstFile;
            return clone;
        }
    }
}