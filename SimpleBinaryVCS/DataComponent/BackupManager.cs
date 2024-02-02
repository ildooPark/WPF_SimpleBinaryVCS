using SimpleBinaryVCS.Model;
using System;
using System.Text.Json; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SimpleBinaryVCS.DataComponent
{
    public class BackupManager
    {
        // Keeps track of all the Project Files, 
        // First tracks the json file in a given Path Directory 
        // if Json file not found, then set the bool to null 
        private bool newProject; 
        public bool NewProject { get { return newProject; } set { newProject = value; } }

        private Dictionary<string, FileBase> projectFiles;
        public Dictionary<string, FileBase> ProjectFiles { get => projectFiles; } 

        public BackupManager()
        {
            projectFiles = new Dictionary<string, FileBase>();
        }
        
        // Save BackUp 
        private void SaveVersion()
        {
            string jsonFile = JsonSerializer.Serialize(projectFiles);
            File.WriteAllText("testFile", jsonFile); 
        }
    }
}