using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using System.IO;

namespace SimpleBinaryVCS.Model
{
    public class ChangedFile : IFile
    {
        private FileChangedState fileChangedState;
        public FileChangedState State
        {
            get => fileChangedState;
            set => fileChangedState = value;
        }
        private string fileSrcPath { get;set; }
        public string FileSrcPath { get => fileSrcPath; set => fileSrcPath = value; }

        private string fileRelPath {  get; set; }
        public string FileRelPath { get => fileRelPath; set => fileRelPath = value; }

        private string fileName {  get; set; }
        public string FileName { get => Path.GetFileName(fileSrcPath); }

        private string? fileHash; 
        public string FileHash 
        {
            get => fileHash ??= "";
            set => fileHash = value; 
        }
        public DateTime changedTime {  get; set; }

        public string FileAbsPath => $"{FileSrcPath}\\{FileRelPath}";

        /// <summary>
        /// Requires getting fileHash Value. 
        /// </summary>
        /// <param name="fileChangedState"></param>
        /// <param name="fileRelPath"></param>
        /// <param name="fileName"></param>
        /// <param name="fileHash"></param>
        public ChangedFile(FileChangedState fileChangedState, string fileSrcPath, string fileRelPath, string fileName)
        {
            this.fileChangedState = fileChangedState;
            this.fileSrcPath = fileSrcPath;
            this.fileRelPath = fileRelPath;
            this.fileName = fileName;
            this.changedTime = DateTime.Now;
        }
    }
}
