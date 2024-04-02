using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployAssistant.Model
{
    public class ProjectSimilarity
    {
        public ProjectData projData;
        public int numberOfDifferences;
        public List<ChangedFile> fileDifferences; 

    }
}
