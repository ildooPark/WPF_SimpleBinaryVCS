using SimpleBinaryVCS.DataComponent;
using System.Text.Json.Serialization;

namespace SimpleBinaryVCS.Model
{
    public class ChangedFile
    {
        public ProjectFile? SrcFile { get; set; }
        public ProjectFile? DstFile { get; set;}
        public DataState DataState {  get; set; }
        public ChangedFile() { }
        public ChangedFile(ChangedFile srcChangedfile)
        {
            if (srcChangedfile.SrcFile != null)
            {
                this.SrcFile = new ProjectFile(srcChangedfile.SrcFile);
                this.SrcFile.IsDstFile = false; 
            }
            else
                this.SrcFile = null;
            if (srcChangedfile.DstFile != null)
            {
                this.DstFile = new ProjectFile(srcChangedfile.DstFile);
                this.DstFile.IsDstFile = true; 
            }
            else
                this.DstFile = null;
            this.DataState = srcChangedfile.DataState;
        }
        public ChangedFile(ProjectFile DstFile, DataState DataState)
        {
            this.SrcFile = null;
            this.DstFile = DstFile;
            DstFile.IsDstFile = true; 
            this.DataState = DataState;
        }
        public ChangedFile(ProjectFile SrcFile,  ProjectFile DstFile, DataState DataState, bool RegisterChanges)
        {
            this.SrcFile = SrcFile;
            SrcFile.IsDstFile = false; 
            this.DstFile = DstFile;
            DstFile.IsDstFile = true;
            this.DataState = DataState;
        }
        [JsonConstructor]
        public ChangedFile(ProjectFile SrcFile, ProjectFile DstFile, DataState DataState)
        {
            this.SrcFile = SrcFile;
            this.DstFile = DstFile;
            this.DataState = DataState;
        }
    }
}
