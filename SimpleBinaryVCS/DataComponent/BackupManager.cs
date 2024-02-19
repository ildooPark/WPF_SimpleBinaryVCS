using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using System.IO;
using System.Runtime.CompilerServices;

namespace SimpleBinaryVCS.DataComponent
{
    public class BackupManager
    {
        // Keeps track of all the Project Files, 
        // First tracks the json file in a given Path Directory 
        // if Json file not found, then set the bool to null 

        private Dictionary<string, IFile> backupFiles;
        /// <summary>
        /// key : file Hash Value 
        /// Value : IFile, which may include TrackedFiles, or ProjectFiles 
        /// </summary>
        public Dictionary<string, IFile> BackupFiles { get => backupFiles; set => backupFiles = value; }

        public Action? RevertAction;

        private VersionControlManager vcsManager; 
        private FileManager fileManager;
        public BackupManager()
        {
            vcsManager = App.VcsManager; 
            fileManager = App.FileManager;
            backupFiles = vcsManager.ProjectRepository.BackupFiles;
            backupFiles = new Dictionary<string, IFile>();

            vcsManager.projectInitialized += 
        }

        // Save BackUp 

        /// <summary>
        /// By Default should Point to ProjectMain, Make backup of the ucrrent project main before applying new changes. 
        /// </summary>
        /// <param name="projectData">Should Point to ProjectMain</param>
        public void MakeProjectBackup (ProjectData projectData)
        {
            // Get changed File List 
            // IF Modified : Try Register new Backup File  
            // IF Added : Skip
            // IF Deleted : Erase from the projectFiles List 
            // IF Restored: 

        }

        private void MakeBackupFile(ProjectData projectData, ProjectFile file)
        {

        }


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
        #region Planned 
        public void MakeProjectFullBackup(ProjectData projectData)
        {

        }
        #endregion
    }
}