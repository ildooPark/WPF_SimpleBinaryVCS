using SimpleBinaryVCS.DataComponent;
using System.Configuration;
using System.Data;
using System.Windows;
using WinFormsApp = System.Windows.Forms.Application; 

namespace SimpleBinaryVCS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static VersionControlManager? vcsManager; 
        public static VersionControlManager VcsManager
        {
            get
            {
                if (vcsManager == null)
                {
                    vcsManager = new VersionControlManager();
                    return vcsManager;
                }
                else
                    return vcsManager;
            }
        }

        private static FileTrackManager? uploaderManager;
        public static FileTrackManager FileTrackManager
        {
            get
            {
                if (uploaderManager == null)
                {
                    uploaderManager = new FileTrackManager();
                    return uploaderManager;
                }
                else
                    return uploaderManager;
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
    }
}