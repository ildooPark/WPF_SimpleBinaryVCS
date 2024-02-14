using System;
using System.Collections.Generic;
using System.IO;
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
        public string fileSrcPath { get;set; }
        public string fileRelPath {  get; set; }
        public string fileName {  get; set; }
        private string? fileHash; 
        public string? FileHash 
        {
            get => fileHash ??= ""; 
            set
            {
                fileHash = value;
                fileHashChecked = true; 
            }
        }
        public DateTime changedTime {  get; set; }
        /// <summary>
        /// Requires getting fileHash Value. 
        /// </summary>
        /// <param name="fileChangedState"></param>
        /// <param name="fileRelPath"></param>
        /// <param name="fileName"></param>
        /// <param name="fileHash"></param>
        public ChangedFile(FileChangedState fileChangedState, string fileSrcPath, string fileRelPath, string fileName)
        {
            this.fileChangedState = fileChangedState;
            this.fileSrcPath = fileSrcPath;
            this.fileRelPath = fileRelPath;
            this.fileName = fileName;
            this.changedTime = DateTime.Now;
            this.fileHashChecked = false;
        }
        public string fileFullPath()
        {
            try
            {
                return Path.Combine(fileSrcPath, fileRelPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't get File's Full Path {ex.Message}");
                return "";
            }
        }
    }
}
