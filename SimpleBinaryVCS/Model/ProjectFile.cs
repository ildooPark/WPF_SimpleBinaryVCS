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
        public string fileSrcPath {  get; set; }
        /// <summary>
        /// RelativePath to the ProjectFolder Directory
        /// </summary>
        public string fileRelPath {  get; set; }
        public string? fileHash { get; set; }
        public string? deployedProjectVersion { get; set; }
        public DateTime updatedTime {  get; set; }
        public FileChangedState fileChangedState { get; set; }

        public ProjectFile() { }
        /// <summary>
        /// Lacks FileHash, DeployedProjectVersion, FileChangedState
        /// </summary>
        /// <param name="isNew"></param>
        /// <param name="fileSize"></param>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        /// <param name="fileVersion"></param>
        public ProjectFile(bool isNew, long fileSize, string fileName, string fileSrcPath, string fileRelPath, string? fileVersion, FileChangedState changedState)
        {
            this.isNew = isNew;
            this.fileSize = fileSize;
            this.fileName = fileName;
            this.fileBuildVersion = fileVersion;
            this.fileSrcPath = fileSrcPath;
            this.fileRelPath = fileRelPath;
            this.updatedTime = DateTime.Now;
            this.fileChangedState = changedState;
        }

        [MemoryPackConstructor]
        public ProjectFile(bool isNew, long fileSize, string fileName, string? fileBuildVersion, string fileSrcPath, string fileRelPath, string fileHash, string? deployedProjectVersion, DateTime updatedTime)
        {
            this.isNew = isNew;
            this.fileSize = fileSize;
            this.fileName = fileName;
            this.fileBuildVersion = fileBuildVersion;
            this.fileSrcPath = fileSrcPath;
            this.fileRelPath = fileRelPath;
            this.fileHash = fileHash;
            this.deployedProjectVersion = deployedProjectVersion;
            this.updatedTime = updatedTime; 
        }
        /// <summary>
        /// Deep Copy of ProjectFile
        /// </summary>
        /// <param name="srcFile">Copying File</param>
        public ProjectFile(ProjectFile srcFile)
        {
            this.isNew = srcFile.isNew;
            this.fileSize = srcFile.fileSize;
            this.fileName = srcFile.fileName;
            this.fileBuildVersion = srcFile.fileBuildVersion;
            this.fileSrcPath= srcFile.fileSrcPath;
            this.fileRelPath = srcFile.fileRelPath;
            this.fileHash = srcFile.fileHash;
            this.deployedProjectVersion = srcFile.deployedProjectVersion;
            this.updatedTime = srcFile.updatedTime;
            this.fileChangedState = srcFile.fileChangedState;
        }

        public ProjectFile(string fileSrcPath, string fileRelPath, string fileHash, FileChangedState state)
        {
            this.isNew = true;
            string fileFullPath = Path.Combine(fileSrcPath, fileRelPath);
            var fileInfo = FileVersionInfo.GetVersionInfo(fileFullPath);
            this.fileSize = new FileInfo(fileFullPath).Length; 
            this.fileBuildVersion = fileInfo.FileVersion;
            this.fileSrcPath = fileSrcPath; 
            this.fileName = Path.GetFileName(fileFullPath);
            this.fileRelPath = fileRelPath;
            this.fileHash= fileHash;
            this.updatedTime = DateTime.Now;
            this.fileChangedState = state;
        }

        /// <summary>
        /// using ChangedFile Class, converts to ProjectFile, Sets isNew to true
        /// </summary>
        /// <param name="changedFile"></param>
        public ProjectFile(ChangedFile changedFile, FileChangedState fileChangedState)
        {
            this.isNew = true;
            this.fileChangedState = fileChangedState; 
            var fileInfo = FileVersionInfo.GetVersionInfo(changedFile.fileFullPath());
            this.fileSize = new FileInfo(changedFile.fileFullPath()).Length;
            this.fileBuildVersion = fileInfo.FileVersion;
            this.fileSrcPath = changedFile.fileSrcPath;
            this.fileName = changedFile.fileName;
            this.fileRelPath = changedFile.fileRelPath;
            this.fileHash = changedFile.FileHash;
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
            //if (other?.fileRelPath == this.fileRelPath) 
            //    return other.fileHash == this.fileHash;
            return other?.fileRelPath == this.fileRelPath;
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

        public string fileFullPath()
        {
            try
            {
                return $"{fileSrcPath}\\{fileRelPath}";
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"Couldn't get File's Full Path {ex.Message}");
                return "";
            }
        }
    }
}