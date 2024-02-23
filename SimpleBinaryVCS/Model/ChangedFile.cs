using MemoryPack;
using SimpleBinaryVCS.DataComponent;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ChangedFile
    {
        public ProjectFile? SrcFile;
        public ProjectFile? DstFile;
        public DataState DataState {  get; set; }
        [MemoryPackConstructor]
        public ChangedFile() { }
        public ChangedFile(ProjectFile DstFile, DataState DataState)
        {
            this.SrcFile = null;
            this.DstFile = DstFile;
            DstFile.IsDstFile = true; 
            this.DataState = DataState;
        }
        public ChangedFile(ProjectFile SrcFile,  ProjectFile DstFile, DataState DataState)
        {
            this.SrcFile = SrcFile;
            SrcFile.IsDstFile = false; 
            this.DstFile = DstFile;
            DstFile.IsDstFile = true;
            this.DataState = DataState;
        }
    }
}
