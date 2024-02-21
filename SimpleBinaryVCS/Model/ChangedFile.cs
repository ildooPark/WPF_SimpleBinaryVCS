using MemoryPack;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ChangedFile
    {
        public ProjectFile? SrcFile;
        public ProjectFile? DstFile;

        [MemoryPackConstructor]
        public ChangedFile() { }
        public ChangedFile(ProjectFile DstFile)
        {
            this.SrcFile = null;
            this.DstFile = DstFile;
            DstFile.IsDstFile = true; 
        }
        public ChangedFile(ProjectFile SrcFile,  ProjectFile DstFile)
        {
            this.SrcFile = SrcFile;
            SrcFile.IsDstFile = false; 
            this.DstFile = DstFile;
            DstFile.IsDstFile = true;
        }
    }
}
