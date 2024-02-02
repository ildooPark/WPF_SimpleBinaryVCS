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
        public string? updaterName {  get; set; }
        public DateTime? updatedTime { get; set; }
        public string? updatedVersion {  get; set; }
        public string? updateLog { get; set; }
        public ObservableCollection<FileBase> projectFiles { get; set; }
        public List<FileBase> diffLog; 
        public ProjectData() { }
    }
}