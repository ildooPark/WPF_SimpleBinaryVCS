using MemoryPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ProjectData
    {
        public string? projectName { get; set; }
        public string? updaterName {  get; set; }
        public DateTime updatedTime { get; set; }
        public string? updatedVersion {  get; set; }
        public string? updateLog { get; set; }
        public int numberOfChanges {  get; set; }
        private ObservableCollection<FileBase> projectFiles; 
        public ObservableCollection<FileBase> ProjectFiles 
        { 
            get
            {
                if (projectFiles == null)
                {
                    projectFiles = new ObservableCollection<FileBase>();
                    return projectFiles;
                }
                else 
                    return projectFiles;
            }
            set
            { 
                projectFiles = value; 
            }
        }
        public ObservableCollection<FileBase> diffLog 
        {  
            get; 
            set; 
        } 
        public ProjectData() 
        { 
            projectFiles = new ObservableCollection<FileBase>();
            diffLog = new ObservableCollection<FileBase>();
        }
    }
}