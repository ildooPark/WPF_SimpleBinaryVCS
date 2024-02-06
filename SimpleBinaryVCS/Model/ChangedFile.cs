using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleBinaryVCS.DataComponent;

namespace SimpleBinaryVCS.Model
{
    public class ChangedFile
    {
        public FileChangedState fileChangedState;
        public string filePath {  get; set; }
        public string fileName {  get; set; }
        public string fileHash { get; set; }
        public DateTime changedTime {  get; set; }
    }
}
