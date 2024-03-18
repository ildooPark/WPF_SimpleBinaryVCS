using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployManager.DataComponent
{
    public class ProjectDependencyManager : IManager
    {
        public event Action<MetaDataState>? IssueEventHandler;
        public const string dpcyMetaDataPath = "";
        public void Awake()
        {

        }

        private void GetProjectDependencies()
        {

        }
    }
}
