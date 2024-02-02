using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class FileBase
    {
        public bool isNew { get; set; }
        public long fileSize { get; set; }
        public string fileName {  get; set; }
        public string? fileVersion {  get; set; }
        public string filePath {  get; set; }
        public DateTime updatedTime {  get; set; }
        public FileBase(bool isNew, long fileSize, string fileName, string filePath, string? fileVersion)
        {
            this.isNew = isNew;
            this.fileSize = fileSize;
            this.fileName = fileName;
            this.fileVersion = fileVersion;
            this.filePath = filePath;
            this.updatedTime = DateTime.Now;
        }

        [MemoryPackConstructor]
        public FileBase(bool isNew, long fileSize, string fileName, string filePath, string? fileVersion, DateTime updatedTime)
        {
            this.isNew = isNew;
            this.fileSize = fileSize;
            this.fileName = fileName;
            this.fileVersion = fileVersion;
            this.filePath = filePath;
            this.updatedTime = updatedTime; 
        }

        /// <summary>
        /// First compares fileVersion, then the updatedTime; 
        /// smaller fileVersion corresponds to newer file
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public int CompareTo(FileBase other)
        {
            if (this.fileVersion == null || other.fileVersion == null)
                return this.updatedTime.CompareTo(other.updatedTime);

            char thisVersion = char.ToLower(this.fileVersion[this.fileVersion.Length - 1]);
            char otherVersion = char.ToLower(other.fileVersion[other.fileVersion.Length - 1]); 
            if (char.IsLetter(thisVersion) && char.IsLetter(otherVersion))
            {
                
            }
            else
            {
                return char.IsLetter(thisVersion) ? -1 : 1;
            }
            return this.fileVersion.CompareTo(other.fileVersion);
        }
        /// <summary>
        /// Checks 1. fileName, 2. fileVersion 
        /// IF all returns as true, then MD5 checksum is used to compute the differences.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(FileBase other)
        {
            if (other.fileName != this.fileName) return false;
            return true;
        }
        /// <summary>
        /// Returns False if not Same
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CheckSize(FileBase other)
        {
            return other.fileSize == this.fileSize;
        }
    }
}