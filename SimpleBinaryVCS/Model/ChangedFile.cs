using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.Model
{
    public class ChangedFile
    {
        public ProjectFile? SrcFile;
        public ProjectFile? DstFile;

        public ChangedFile(ProjectFile DstFile)
        {
            this.SrcFile = null;
            this.DstFile = DstFile;
            DstFile.IsDst = true; 
        }
        public ChangedFile(ProjectFile SrcFile,  ProjectFile DstFile)
        {
            this.SrcFile = SrcFile;
            SrcFile.IsDst = false; 
            this.DstFile = DstFile;
            DstFile.IsDst = true;
        }
    }
}
