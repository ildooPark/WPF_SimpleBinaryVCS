using SimpleBinaryVCS.DataComponent;

namespace SimpleBinaryVCS.Interfaces
{
    public interface IFile
    {
        public FileChangedState FileState { get; set; }
        public string FileHash { get; set; }
        public string FileSrcPath { get; }
        public string FileRelPath { get; }
        public string FileName { get; }
        public string FileAbsPath {  get; } 
    }
}