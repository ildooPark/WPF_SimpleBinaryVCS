using SimpleBinaryVCS.DataComponent;

namespace SimpleBinaryVCS.Interfaces
{
    public interface IFile
    {
        public FileChangedState State { get; set; }
        public string FileHash { get; set; }
        public string FileSrcPath { get; set; }
        public string FileRelPath { get; set; }
        public string FileName { get; }
        public string FileAbsPath {  get; } 
    }
}