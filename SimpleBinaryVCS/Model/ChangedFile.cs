using MemoryPack;
using SimpleBinaryVCS.DataComponent;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ChangedFile
    {
        public ProjectFile? SrcFile;
        public ProjectFile? DstFile;
        public DataChangedState DataState {  get; set; }
        [MemoryPackConstructor]
        public ChangedFile() { }
        public ChangedFile(ProjectFile DstFile, DataChangedState DataState)
        {
            this.SrcFile = null;
            this.DstFile = DstFile;
            DstFile.IsDstFile = true; 
            this.DataState = DataState;
        }
        public ChangedFile(ProjectFile SrcFile,  ProjectFile DstFile, DataChangedState DataState)
        {
            this.SrcFile = SrcFile;
            SrcFile.IsDstFile = false; 
            this.DstFile = DstFile;
            DstFile.IsDstFile = true;
            this.DataState = DataState;
        }
    }
}
