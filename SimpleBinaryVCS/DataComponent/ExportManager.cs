using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using WPF = System.Windows;
using System.IO;
using System.IO.Compression;

namespace SimpleBinaryVCS.DataComponent
{
    public class ExportManager : IManager
    {
        private string? _currentProjectPath; 
        private Dictionary<string, ProjectFile>? _backupFilesDict;
        private FileHandlerTool _fileHandlerTool;
        public ExportManager() 
        {
            _fileHandlerTool = App.FileHandlerTool;
        }
        #region Manager Events
        /// <summary>
        /// string = Export Project Path
        /// </summary>
        public event Action<string>? ExportCompleteEventHandler;
        public event Action<MetaDataState>? IssueEventHandler;
        #endregion
        public void Awake(){}
        public void ExportProjectVersionLog(ProjectData projectData)
        {
            string exportDstPath = GetExportProjectPath(projectData);
            string exportVersionLogPath = $"{exportDstPath}\\ProjectVersionLog.bin";
            bool exportResult = false;
            while (!exportResult)
            {
                if (!Directory.Exists(exportDstPath)) Directory.CreateDirectory(exportDstPath);
                exportResult = _fileHandlerTool.TrySerializeProjectData(projectData, exportVersionLogPath);
                if (!exportResult)
                {
                    var response = WPF.MessageBox.Show("Export Failed, Would you like to try again?", "Export Project Version Log", WPF.MessageBoxButton.YesNo);
                    if (response == WPF.MessageBoxResult.Yes)
                    {
                        continue;
                    }
                    else
                    {
                        WPF.MessageBox.Show("Export Canceled");
                        return;
                    }
                }
            }
            ExportCompleteEventHandler?.Invoke(exportDstPath);
        }

        public void ExportProject(ProjectData projectData)
        {
            if (_backupFilesDict == null)
            {
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                WPF.MessageBox.Show("Backup files are missing!, Make sure ProjectMetaData is Set");
                return;
            }
            IssueEventHandler?.Invoke(MetaDataState.Exporting);
            bool exportResult = false;
            string? exportPath = null; 
            while (!exportResult)
            {
                exportResult = TryExportProject(projectData, out exportPath);
                if (!exportResult)
                {
                    var response = WPF.MessageBox.Show("Export Failed, Would you like to try again?", "Export Project", WPF.MessageBoxButton.YesNo);
                    if (response == WPF.MessageBoxResult.Yes)
                    {
                        continue;
                    }
                    else
                    {
                        WPF.MessageBox.Show("Export Canceled");
                        IssueEventHandler?.Invoke(MetaDataState.Idle);
                        break;
                    }
                }
            }
            if (exportPath != null)
            {
                IssueEventHandler?.Invoke(MetaDataState.Idle);
                ExportCompleteEventHandler?.Invoke(exportPath);
            }
        }
        private bool TryExportProject(ProjectData projectData, out string? exportPath)
        {
            try
            {
                if (_backupFilesDict == null)
                {
                    WPF.MessageBox.Show("Backup files are missing!, Make sure ProjectMetaData is Set");
                    exportPath = null; return false;
                }
                string exportDstPath = GetExportProjectPath(projectData);
                string exportZipPath = $"{exportDstPath}.zip"; 
                int exportCount = 0;

                foreach (ProjectFile file in projectData.ProjectFiles.Values)
                {
                    if (file.DataType == ProjectDataType.Directory)
                    {

                        bool handleResult = _fileHandlerTool.HandleDirectory(null, Path.Combine(exportDstPath, file.DataRelPath), DataState.None);
                        if (!handleResult)
                        {
                            WPF.MessageBox.Show($"Export Failed! for file {file.DataName}!");
                            exportPath = null;  return false;
                        }
                        exportCount++;
                        continue;
                    }
                    if (!_backupFilesDict.TryGetValue(file.DataHash, out ProjectFile? backupFile))
                    {
                        WPF.MessageBox.Show($"Export Failed! for file {file.DataName}!");
                        exportPath = null; return false;
                    }
                    else
                    {
                        bool handleResult = _fileHandlerTool.HandleFile(backupFile.DataAbsPath, Path.Combine(exportDstPath, file.DataRelPath), DataState.None);
                        if (!handleResult)
                        {
                            WPF.MessageBox.Show($"Export Failed! for file {file.DataName}!");
                            exportPath = null; return false;
                        }
                        exportCount++;
                    }
                }
                if (exportCount <= 0)
                {
                    exportPath = null; return false;
                }
                ZipFile.CreateFromDirectory(exportDstPath, exportZipPath);
                exportPath = Directory.GetParent(exportDstPath)?.ToString();  return true; 
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show(ex.Message);
                exportPath = null; return false;
            }
        }

        public void ExportProjectChanges(ProjectData projectData, List<ChangedFile> changes)
        {

        }
        private bool TryExportProjectChanges(ProjectData projectData, List<ChangedFile> changes, out string? exportPath)
        {
            exportPath = null;
            return false; 
        }

        private string GetExportProjectPath(ProjectData projectData)
        {
            return $"{_currentProjectPath}\\Export_{projectData.ProjectName}\\{projectData.UpdatedVersion}";
        }
        #region CallBacks From Parent Model 
        public void MetaDataLoadedCallBack(object metaDataObj)
        {
            if (metaDataObj is not ProjectMetaData projectMetaData) return;
            if (projectMetaData == null) return;
            this._backupFilesDict = projectMetaData.BackupFiles;
            this._currentProjectPath = projectMetaData.ProjectPath;
        }
        #endregion
    }
}
