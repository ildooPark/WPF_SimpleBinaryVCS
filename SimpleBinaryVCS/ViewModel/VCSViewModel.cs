using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using System;
using WinForms = System.Windows.Forms; 
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json; 
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.TeamFoundation.MVVM;
using Microsoft.TeamFoundation.Build.Client;
using System.IO;
using System.Diagnostics;
using MemoryPack;

namespace SimpleBinaryVCS.ViewModel
{
    public class VCSViewModel : ViewModelBase
    {
        private ProjectData nextProjectVersion; 
        public ProjectData NextProjectVersion
        {
            get { return nextProjectVersion; }
            set
            {
                nextProjectVersion = value;
            }
        }

        private ObservableCollection<FileBase> projectFiles;
        public ObservableCollection<FileBase> ProjectFiles => projectFiles;
        private ICommand conductUpdate;
        private ICommand getProject;
        public ICommand ConductUpdate
        {
            get
            {
                if (conductUpdate == null) conductUpdate = new RelayCommand(UpdateProject, CanUpdate);
                return conductUpdate;
            }
        }
        public ICommand GetProject
        {
            get
            {
                if (getProject == null) getProject = new RelayCommand(RetrieveProject, CanExecute);
                return getProject;
            }
        }
        public VCSViewModel()
        {
            nextProjectVersion = App.VcsManager.ProjectData; 
            projectFiles = App.VcsManager.ProjectData.ProjectFiles; 
        }

        private void UpdateProject(object obj)
        {
            if (nextProjectVersion.updatedVersion == null || nextProjectVersion.updaterName == null)
            {
                var response = MessageBox.Show("Must Have both Deploy Version AND UpdaterName", "ok", MessageBoxButtons.OK);
                if (response == DialogResult.OK) return; 
            }
            foreach (FileBase uploadedFile in App.UploaderManager.UploadedFileList)
            {
                if (projectFiles.Contains(uploadedFile))
                {
                    FileBase? srcFile = projectFiles.FirstOrDefault(x => x == uploadedFile); 
                    if (srcFile != null)
                    {
                        (string?, string?) resultingHash; 
                        bool result = VersionControlManager.TryCompareMD5CheckSum(srcFile.filePath, uploadedFile.filePath, out resultingHash);
                        Console.WriteLine($"Hash Values, {resultingHash.Item1} {resultingHash.Item2}");
                        App.VcsManager.ProjectData.diffLog.Add(uploadedFile);
                        App.VcsManager.ProjectData.diffLog.Add(srcFile);
                    }
                }
                else
                {
                    // 
                    string? newFileHash = VersionControlManager.GetMD5CheckSum(uploadedFile.filePath);
                    if (newFileHash != null)
                    {
                        App.VcsManager.ProjectData.diffLog.Add(uploadedFile);
                    }
                }
            }
            //App.VcsManager.
            return;
        }

        private bool CanUpdate(object obj)
        {
            if (projectFiles == null || projectFiles.Count == 0 || App.UploaderManager.UploadedFileList.Count == 0) return false;
            if (nextProjectVersion.updatedVersion == null || nextProjectVersion.updaterName == null)
            {
                return false;
            }
            return true;
        }

        private bool CanExecute(object parameter)
        {
            return true;
        }

        private void RetrieveProject(object parameter)
        {
            if (projectFiles != null && projectFiles.Count != 0) projectFiles.Clear();
            var openFD = new WinForms.FolderBrowserDialog();
            string projectDataBin; 
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                App.VcsManager.ProjectPath = openFD.SelectedPath;
                App.VcsManager.ProjectData.projectName = Path.GetFileName(openFD.SelectedPath);
            }

            //Get .bin VersionLog File 
            string[] binFiles = Directory.GetFiles(openFD.SelectedPath, "BinaryVersionLog.*", SearchOption.AllDirectories);

            if (binFiles.Length > 0)
            {
                projectDataBin = binFiles[0];
                ProjectData? currentData;
                try
                {
                    var stream = File.ReadAllBytes(projectDataBin);
                    currentData = MemoryPackSerializer.Deserialize<ProjectData>(stream); 
                    if (currentData != null)
                    {
                        nextProjectVersion = currentData;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            else
            {
                var result = MessageBox.Show("BinaryVersionLog file not found!\n Initialize New Project?",
                    "Import Project", MessageBoxButtons.YesNo); 
                if (result == DialogResult.Yes)
                {
                    //Parse 
                    string[]? newProjectFiles; TryGetAllFiles(openFD.SelectedPath, out newProjectFiles);
                    if (newProjectFiles == null) return;
                    foreach (string filePath in newProjectFiles)
                    {
                        var fileInfo = FileVersionInfo.GetVersionInfo(filePath);
                        FileBase newFile = new FileBase(true, new FileInfo(filePath).Length, fileInfo.FileName, filePath, fileInfo.FileVersion);
                        projectFiles?.Add(newFile);
                    }
                }
                else
                {
                    MessageBox.Show("Select New Project Path"); 
                    return; 
                }
            }
        }

        private void TryGetAllFiles(string directoryPath, out string[]? Files)
        {
            try
            {
                Files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Files = null; 
            }
        }
    }
}
