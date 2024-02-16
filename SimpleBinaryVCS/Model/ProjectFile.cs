using MemoryPack;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
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
    public partial class ProjectFile : IEquatable<ProjectFile>, IFile
    {
        public bool IsNew { get; set; }
        public long FileSize { get; set; }
        public string FileBuildVersion {  get; set; }
        [MemoryPackInclude]
        private string fileName;
        [MemoryPackInclude]
        private string fileSrcPath;
        [MemoryPackInclude]
        private string fileRelPath;
        [MemoryPackInclude]
        private string fileHash;
        public string DeployedProjectVersion { get; set; }
        public DateTime UpdatedTime {  get; set; }
        [MemoryPackInclude]
        private FileChangedState fileState;
        [MemoryPackIgnore]
        public string FileSrcPath => fileSrcPath;
        [MemoryPackIgnore]
        public string FileRelPath => fileRelPath;
        [MemoryPackIgnore]
        public string FileName => fileName;
        [MemoryPackIgnore]
        public string FileAbsPath => $"{FileSrcPath}\\{FileRelPath}";
        [MemoryPackIgnore]
        public FileChangedState FileState { get => fileState; set => fileState = value; }
        [MemoryPackIgnore]
        public string FileHash { get => fileHash; set => fileHash = value; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProjectFile() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>
        /// Lacks FileHash, DeployedProjectVersion, FileChangedState
        /// </summary>
        /// <param name="isNew"></param>
        /// <param name="fileSize"></param>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        /// <param name="fileVersion"></param>
        public ProjectFile(bool isNew, long fileSize, string? fileVersion, string fileName, string fileSrcPath, string fileRelPath, FileChangedState changedState)
        {
            this.IsNew = isNew;
            this.FileSize = fileSize;
            this.FileBuildVersion = fileVersion ?? "";
            this.fileName = fileName;
            this.fileSrcPath = fileSrcPath;
            this.fileRelPath = fileRelPath;
            this.fileHash = "";
            this.DeployedProjectVersion = "";
            this.UpdatedTime = DateTime.Now;
            this.fileState = changedState;
        }

        [MemoryPackConstructor]
        public ProjectFile(bool isNew, long fileSize, string fileName, string? fileBuildVersion, string fileSrcPath, string fileRelPath, string fileHash, string deployedProjectVersion, DateTime updatedTime, FileChangedState fileState)
        {
            this.IsNew = isNew;
            this.FileSize = fileSize;
            this.fileName = fileName;
            this.FileBuildVersion = fileBuildVersion ?? "";
            this.fileSrcPath = fileSrcPath;
            this.fileRelPath = fileRelPath;
            this.fileHash = fileHash;
            this.DeployedProjectVersion = deployedProjectVersion;
            this.UpdatedTime = updatedTime; 
            this.fileState = fileState;
        }
        /// <summary>
        /// Deep Copy of ProjectFile
        /// </summary>
        /// <param name="srcFile">Copying File</param>
        public ProjectFile(ProjectFile srcFile)
        {
            this.IsNew = srcFile.IsNew;
            this.FileSize = srcFile.FileSize;
            this.fileName = srcFile.fileName;
            this.FileBuildVersion = srcFile.FileBuildVersion;
            this.fileSrcPath= srcFile.fileSrcPath;
            this.fileRelPath = srcFile.fileRelPath;
            this.fileHash = srcFile.fileHash;
            this.DeployedProjectVersion = srcFile.DeployedProjectVersion;
            this.UpdatedTime = srcFile.UpdatedTime;
            this.fileState = srcFile.fileState;
        }

        public ProjectFile(string fileSrcPath, string fileRelPath, string fileHash, FileChangedState state)
        {
            this.IsNew = true;
            string fileFullPath = Path.Combine(fileSrcPath, fileRelPath);
            var fileInfo = FileVersionInfo.GetVersionInfo(fileFullPath);
            this.FileSize = new FileInfo(fileFullPath).Length; 
            this.FileBuildVersion = fileInfo.FileVersion ?? "";
            this.DeployedProjectVersion = "";
            this.fileSrcPath = fileSrcPath; 
            this.fileName = Path.GetFileName(fileFullPath);
            this.fileRelPath = fileRelPath;
            this.fileHash= fileHash;
            this.UpdatedTime = DateTime.Now;
            this.fileState = state;
        }

        /// <summary>
        /// using ChangedFile Class, converts to ProjectFile, Sets isNew to true
        /// </summary>
        /// <param name="changedFile"></param>
        public ProjectFile(TrackedFile changedFile, FileChangedState fileChangedState)
        {
            this.IsNew = true;
            this.fileState = fileChangedState; 
            var fileInfo = FileVersionInfo.GetVersionInfo(changedFile.FileAbsPath);
            this.FileSize = new FileInfo(changedFile.FileAbsPath).Length;
            this.FileBuildVersion = fileInfo.FileVersion ?? "";
            this.DeployedProjectVersion = "";
            this.fileSrcPath = changedFile.FileSrcPath;
            this.fileName = changedFile.FileName;
            this.fileRelPath = changedFile.FileRelPath;
            this.fileHash = changedFile.FileHash;
            this.UpdatedTime = changedFile.changedTime;
        }
        /// <summary>
        /// First compares fileVersion, then the updatedTime; 
        /// smaller fileVersion corresponds to newer file
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ProjectFile other) 
        {
            if (this.UpdatedTime.CompareTo(other.UpdatedTime) == 0)
                return this.FileSize.CompareTo(other.FileSize);
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
            return other.FileSize == this.FileSize;
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