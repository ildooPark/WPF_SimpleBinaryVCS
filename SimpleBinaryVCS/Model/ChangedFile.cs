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
        public bool fileHashChecked; 
        public string filePath {  get; set; }
        public string fileName {  get; set; }
        public string? fileHash 
        {
            get => fileHash; 
            set
            {
                fileHash = value;
                fileHashChecked = true; 
            }
        }
        public DateTime changedTime {  get; set; }
        public DateTime lastRead { get; set; }
        /// <summary>
        /// Requires getting fileHash Value. 
        /// </summary>
        /// <param name="fileChangedState"></param>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="fileHash"></param>
        public ChangedFile(FileChangedState fileChangedState, string filePath, string fileName)
        {
            this.fileChangedState = fileChangedState;
            this.filePath = filePath;
            this.fileName = fileName;
            this.changedTime = DateTime.Now;
            this.fileHashChecked = false;
        }
    }
}
