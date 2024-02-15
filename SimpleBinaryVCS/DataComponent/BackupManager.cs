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

        private Dictionary<string, ProjectFile> projectFiles;
        public Dictionary<string, ProjectFile> ProjectFiles { get => projectFiles; }

        public Action? RevertAction; 
        public BackupManager()
        {
            projectFiles = new Dictionary<string, ProjectFile>();
        }

        // Save BackUp 
        private void SaveVersion()
        {
            string jsonFile = JsonSerializer.Serialize(projectFiles);
            File.WriteAllText("testFile", jsonFile);
        }

        public string GetBackupPath(string parentPath, string projectName,  string projectVersion)
        {
            string backupPath = $"{parentPath}\\Backup_{Path.GetFileName(projectName)}\\Backup_{projectVersion}";
            return backupPath; 
        }

    }
}