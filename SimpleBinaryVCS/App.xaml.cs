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
        private static VersionControlManager vcsManager; 
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
    }
}
