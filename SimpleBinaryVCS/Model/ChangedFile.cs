using MemoryPack;
using SimpleBinaryVCS.DataComponent;
using System.Text.Json.Serialization;

namespace SimpleBinaryVCS.Model
{
    public class ChangedFile: ICloneable
    {
        public ProjectFile? SrcFile;
        public ProjectFile? DstFile;
        public DataState DataState {  get; set; }
        [JsonConstructor]
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

        public object Clone()
        {
            ChangedFile clone = new ChangedFile();
            if (this.SrcFile != null) clone.SrcFile = (ProjectFile) this.SrcFile.Clone();
            if (this.DstFile != null) clone.DstFile = (ProjectFile) this.DstFile.Clone();
            clone.DataState = this.DataState;
            return clone;
        }
    }
}
