using SimpleBinaryVCS.DataComponent;

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
            get => metaDataManager ??= new MetaDataManager();
        }

        private static UpdateManager? updateManager;
        public static UpdateManager UpdateManager
        {
            get => updateManager ??= new UpdateManager();
        }

        private static BackupManager? backupManager;
        public static BackupManager BackupManager
        {
            get => backupManager ??= new BackupManager();
        }

        private static FileManager? fileManager;
        public static FileManager FileManager
        {
            get => fileManager ??= new FileManager();
        }

        public static void AwakeManagers()
        {
            MetaDataManager.Awake(); 
            UpdateManager.Awake();
            BackupManager.Awake();
            FileManager.Awake();
        }
    }
}