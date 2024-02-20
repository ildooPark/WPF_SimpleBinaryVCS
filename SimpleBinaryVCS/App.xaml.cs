using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;

namespace SimpleBinaryVCS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static MetaDataManager? metaDataManager; 
        public static MetaDataManager MetaDataManager
        {
            get
            {
                if (metaDataManager == null)
                {
                    metaDataManager = new MetaDataManager();
                    return metaDataManager;
                }
                else
                    return metaDataManager;
            }
        }

        private static BackupManager? backupManager;
        public static BackupManager BackupManager
        {
            get
            {
                if (backupManager == null)
                {
                    backupManager = new BackupManager();
                    return backupManager;
                }
                else
                    return backupManager;
            }
        }

        private static FileManager? fileManager;
        public static FileManager FileManager
        {
            get
            {
                if (fileManager == null)
                {
                    fileManager = new FileManager();
                    return fileManager;
                }
                else 
                    return fileManager;
            }
        }

        public static void AwakeModel()
        {
            MetaDataManager.Awake(); 
            BackupManager.Awake();
            FileManager.Awake();
        }
    }
}