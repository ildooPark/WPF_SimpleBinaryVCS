using MemoryPack;
using SimpleBinaryVCS.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ProjectRepository
    {
        private ProjectData projectMain;
        public ProjectData ProjectMain { get; set; }
        private LinkedList<ProjectData>? projectDataList;
        public LinkedList<ProjectData> ProjectDataList
        {
            get => projectDataList ??= (projectDataList = new LinkedList<ProjectData>());
            set => projectDataList = value;
        }
        public Dictionary<string, IFile> backupFiles;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProjectRepository() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
