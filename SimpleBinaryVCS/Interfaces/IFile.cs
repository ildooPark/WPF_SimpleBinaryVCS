using SimpleBinaryVCS.DataComponent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.Interfaces
{
    public interface IFile
    {
        public FileChangedState State { get; set; }
        public string FileSrcPath { get; set; }
        public string FileRelPath { get; set; }
        public string FileName { get => Path.GetFileName(FileSrcPath); }
        public string FileAbsPath {  get=> Path.Combine(FileSrcPath, FileRelPath); } 
    }
}