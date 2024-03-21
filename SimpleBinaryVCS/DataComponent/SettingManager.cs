using DeployManager.Model;
using SimpleBinaryVCS;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.IO;

namespace DeployManager.DataComponent
{
    public class SettingManager : IManager
    {
        public event Action<MetaDataState>? ManagerStateEventHandler;
        public event Action<string>? SetLastDstProject; 
        public readonly string? settingDataPath;
        public readonly string? DAMetaFilePath;
        public const string DAMetaFilename = "DeployAssistant.config";
        private FileHandlerTool _fileHandlerTool;
        public SettingManager()
        {
            try
            {
                settingDataPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                DAMetaFilePath = Path.Combine(settingDataPath, DAMetaFilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            _fileHandlerTool = App.FileHandlerTool;
        }
        public void Awake()
        {
            try
            {
                if (File.Exists(DAMetaFilePath))
                {
                    if (!_fileHandlerTool.TryDeserializeJsonData(DAMetaFilePath, out LocalConfigData? localConfigData)) return;
                    var result = MessageBox.Show($"Recent Destination Project Path Found: Proceed with this Destination? {localConfigData.LastOpenedDstPath}",
                        "Import Previous Destination Project", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        SetLastDstProject?.Invoke(localConfigData.LastOpenedDstPath);
                    }
                    else
                    {
                        MessageBox.Show("Couldn't Retrieve ");
                        return;
                    }
                }
            }
            //GetConfigData
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        public void SetRecentDstDirectory(string dstPath)
        {
            LocalConfigData localConfig = new LocalConfigData(dstPath);
            _fileHandlerTool.TrySerializeJsonData(DAMetaFilePath, localConfig);
        }
    }
}
