using MemoryPack;
using SimpleBinaryVCS.DataComponent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ProjectFile : IEquatable<ProjectFile>
    {
        public bool isNew { get; set; }
        public long fileSize { get; set; }
        public string fileName {  get; set; }
        /// <summary>
        /// File Build Version
        /// </summary>
        public string? fileBuildVersion {  get; set; }
        /// <summary>
        /// RelativePath to the ProjectFolder Directory
        /// </summary>
        public string filePath {  get; set; }
        public string? fileHash { get; set; }
        public string? deployedProjectVersion { get; set; }
        public FileChangedState fileChangedState;
        public DateTime updatedTime {  get; set; }
        public ProjectFile() { }
        /// <summary>
        /// Lacks FileHash, DeployedProjectVersion, FileChangedState
        /// </summary>
        /// <param name="isNew"></param>
        /// <param name="fileSize"></param>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        /// <param name="fileVersion"></param>
        public ProjectFile(bool isNew, long fileSize, string fileName, string filePath, string? fileVersion)
        {
            this.isNew = isNew;
            this.fileSize = fileSize;
            this.fileName = fileName;
            this.fileBuildVersion = fileVersion;
            this.filePath = filePath;
            this.updatedTime = DateTime.Now;
        }

        [MemoryPackConstructor]
        public ProjectFile(bool isNew, long fileSize, string fileName, string? fileBuildVersion, string filePath, string fileHash, string? deployedProjectVersion, DateTime updatedTime)
        {
            this.isNew = isNew;
            this.fileSize = fileSize;
            this.fileName = fileName;
            this.fileBuildVersion = fileBuildVersion;
            this.filePath = filePath;
            this.fileHash = fileHash;
            this.deployedProjectVersion = deployedProjectVersion;
            this.updatedTime = updatedTime; 
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcFile">Deep Copy of ProjectFile</param>
        public ProjectFile(ProjectFile srcFile)
        {
            this.isNew = srcFile.isNew;
            this.fileSize = srcFile.fileSize;
            this.fileName = srcFile.fileName;
            this.fileBuildVersion = srcFile.fileBuildVersion;
            this.filePath = srcFile.filePath;
            this.fileHash = srcFile.fileHash;
            this.deployedProjectVersion = srcFile.deployedProjectVersion;
            this.updatedTime = srcFile.updatedTime;
        }

        /// <summary>
        /// using ChangedFile Class, converts to ProjectFile
        /// </summary>
        /// <param name="changedFile"></param>
        public ProjectFile(ChangedFile changedFile)
        {
            this.isNew = true;
            fileChangedState = changedFile.fileChangedState;
            var fileInfo = FileVersionInfo.GetVersionInfo(changedFile.filePath);
            this.fileSize = new FileInfo(changedFile.filePath).Length;
            this.fileBuildVersion = fileInfo.FileVersion;
            this.fileName = changedFile.fileName;
            this.filePath = changedFile.filePath;
            this.fileHash = changedFile.fileHash;
            this.updatedTime = changedFile.changedTime;
        }
        /// <summary>
        /// First compares fileVersion, then the updatedTime; 
        /// smaller fileVersion corresponds to newer file
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ProjectFile other) 
        {
            if (this.updatedTime.CompareTo(other.updatedTime) == 0)
                return this.fileSize.CompareTo(other.fileSize);
            return this.updatedTime.CompareTo(other.updatedTime);
        }
        /// <summary>
        /// Checks 1. fileName, 2. fileVersion 
        /// IF all returns as true, then MD5 checksum is used to compute the differences.
        /// </summary>
        /// <param name = "other" ></ param >
        /// < returns ></ returns >
        public bool Equals(ProjectFile? other)
        {
            return other?.fileName == this.fileName;
        }
        /// <summary>
        /// Returns False if not Same
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CheckSize(ProjectFile other)
        {
            return other.fileSize == this.fileSize;
        }
    }
}