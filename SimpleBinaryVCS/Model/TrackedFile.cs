using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using System.IO;

namespace SimpleBinaryVCS.Model
{
    public class TrackedFile : IFile
    {
        private FileChangedState fileChangedState;
        public FileChangedState FileState{ get => fileChangedState; set => fileChangedState = value;}

        private readonly string fileSrcPath;
        public string FileSrcPath => fileSrcPath;


        private readonly string fileRelPath;
        public string FileRelPath => fileRelPath;

        private readonly string fileName;
        public string FileName => fileName;

        public string FileAbsPath => $"{FileSrcPath}\\{FileRelPath}";

        private string? fileHash; 
        public string FileHash { get => fileHash ??= ""; set => fileHash = value; }

        public DateTime changedTime { get; set; }


        /// <summary>
        /// Requires getting fileHash Value. 
        /// </summary>
        /// <param name="fileChangedState"></param>
        /// <param name="fileRelPath"></param>
        /// <param name="fileName"></param>
        /// <param name="fileHash"></param>
        public TrackedFile(FileChangedState fileChangedState, string fileSrcPath, string fileRelPath, string fileName)
        {
            this.fileChangedState = fileChangedState;
            this.fileSrcPath = fileSrcPath;
            this.fileRelPath = fileRelPath;
            this.fileName = fileName;
            this.changedTime = DateTime.Now;
        }
    }
}
