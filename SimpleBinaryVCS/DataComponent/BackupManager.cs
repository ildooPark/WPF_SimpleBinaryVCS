using SimpleBinaryVCS.Interfaces;
using System.IO;

namespace SimpleBinaryVCS.DataComponent
{
    public class BackupManager
    {
        // Keeps track of all the Project Files, 
        // First tracks the json file in a given Path Directory 
        // if Json file not found, then set the bool to null 

        private Dictionary<string, IFile> backupFiles;
        public Dictionary<string, IFile> BackupFiles { get => backupFiles; set => backupFiles = value; }

        public Action? RevertAction; 
        public BackupManager()
        {
            backupFiles = new Dictionary<string, IFile>();
        }

        // Save BackUp 

        public string GetFileBackupPath(string parentPath, string projectName,  string projectVersion)
        {
            string backupPath = $"{parentPath}\\Backup_{Path.GetFileName(projectName)}\\Backup_{projectVersion}";
            return backupPath; 
        }

        public void UpdateBackupData(IFile backupFile)
        {
            //Try Adding 
            //Else, Update Backup Path Info 

        }
    }
}