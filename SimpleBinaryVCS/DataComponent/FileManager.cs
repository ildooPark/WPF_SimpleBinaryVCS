using MemoryPack;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using WPF = System.Windows;

namespace SimpleBinaryVCS.DataComponent
{
    [Flags]
    public enum DataState
    {
        None = 0,
        Added = 1,
        Deleted = 1 << 1,
        Restored = 1 << 2,
        Modified = 1 << 3,
        PreStaged = 1 << 4,
        IntegrityChecked = 1 << 5, 
        Backup = 1 << 6
    }
    public class FileManager : IManager
    {
        public event Action<object>? DataStagedEventHandler;
        public event Action<object>? DataPreStagedEventHandler;
        public event Action<object>? SrcProjectDataEventHandler;
        public event Action<object>? PreStagedDataOverlapEventHandler;
        public event Action<object, string, ObservableCollection<ProjectFile>>? IntegrityCheckEventHandler;

        private Dictionary<string, ProjectFile> backupFiles;
        private Dictionary<string, ProjectFile> projectFilesDict;
        private Dictionary<string, ProjectFile> registeredFilesDict;
        private SemaphoreSlim asyncControl;
        
        private ObservableCollection<ProjectFile> changedFileList;
        public ObservableCollection<ProjectFile> ChangedFileList
        {
            get => changedFileList ??= new ObservableCollection<ProjectFile>();
            set => changedFileList = value;
        }

        private ProjectData? currentProjectData;
        public ProjectData? CurrentProjectData
        {
            get
            {
                if (currentProjectData == null)
                {
                    MessageBox.Show("Project Data is unreachable from FileManager");
                    return null; 
                }
                return currentProjectData;
            }
        }

        private FileHandlerTool fileHandlerTool;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public FileManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            changedFileList = new ObservableCollection<ProjectFile>();
            projectFilesDict = new Dictionary<string, ProjectFile>();
            registeredFilesDict = new Dictionary<string, ProjectFile>();
            fileHandlerTool = new FileHandlerTool();    
            asyncControl = new SemaphoreSlim(5); 
        }
        public void Awake()
        {
        }
        #region Identifying file differences against given version(s)
        private async Task HashPreStagedFilesAsync()
        {
            try
            {
                if (registeredFilesDict.Count <= 0) return;
                List<Task> asyncTasks = new List<Task>();
                //Update changedFilesDict
                foreach (ProjectFile file in registeredFilesDict.Values)
                {
                    if (file.DataType == ProjectDataType.Directory || file.DataHash != "") continue;
                    asyncTasks.Add(Task.Run(async () =>
                    {
                        await asyncControl.WaitAsync();
                        try
                        {
                            await HashTool.GetFileMD5CheckSumAsync(file);
                        }
                        finally
                        {
                            asyncControl.Release();
                        }
                    }));
                }
                await Task.WhenAll(asyncTasks);
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"File Manager UpdateHashFromChangedList Error: {ex.Message}");
            }
        }
        public void PerformIntegrityCheck(object sender)
        {
            MainProjectIntegrityCheck(sender);
        }
        private void MainProjectIntegrityCheck(object sender)
        {
            ProjectData? mainProject = currentProjectData;
            if (mainProject == null)
            {
                MessageBox.Show("Main Project is Missing");
                return;
            }
            registeredFilesDict.Clear();
            changedFileList.Clear(); 
            try
            {
                StringBuilder fileIntegrityLog = new StringBuilder();
                List<ChangedFile> fileChanges = new List<ChangedFile>();
                fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {mainProject.UpdatedVersion}");

                Dictionary<string, ProjectFile> projectFilesDict = mainProject.ProjectFiles;

                List<string> recordedFiles = mainProject.ProjectRelFilePathsList;
                List<string> recordedDirs = mainProject.ProjectRelDirsList;

                List<string> directoryFiles = new List<string>();
                List<string> directoryDirs = new List<string>();

                string[]? rawFiles = Directory.GetFiles(mainProject.ProjectPath, "*", SearchOption.AllDirectories);
                foreach (string absPathFile in rawFiles)
                {
                    directoryFiles.Add(Path.GetRelativePath(mainProject.ProjectPath, absPathFile));
                }
                
                string[]? rawDirs = Directory.GetDirectories(mainProject.ProjectPath, "*", SearchOption.AllDirectories);
                foreach (string absPathFile in rawDirs)
                {
                    directoryDirs.Add(Path.GetRelativePath(mainProject.ProjectPath, absPathFile));
                }
                
                IEnumerable<string> addedFiles = directoryFiles.Except(recordedFiles);
                IEnumerable<string> addedDirs = directoryDirs.Except(recordedDirs);
                IEnumerable<string> deletedFiles = recordedFiles.Except(directoryFiles);
                IEnumerable<string> deletedDirs = recordedDirs.Except(directoryDirs);
                IEnumerable<string> intersectFiles = recordedFiles.Intersect(directoryFiles);

                foreach (string dirRelPath in addedDirs)
                {
                    ProjectFile dstFile = new ProjectFile(mainProject.ProjectPath, dirRelPath, null, DataState.Added | DataState.IntegrityChecked, ProjectDataType.Directory);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Added | DataState.IntegrityChecked);
                    fileChanges.Add(newChange);
                    changedFileList.Add(dstFile);
                }

                foreach (string dirRelPath in deletedDirs)
                {
                    ProjectFile dstFile = new ProjectFile(mainProject.ProjectPath, dirRelPath, null, DataState.Deleted | DataState.IntegrityChecked, ProjectDataType.Directory);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Deleted | DataState.IntegrityChecked);
                    fileChanges.Add(newChange);
                    changedFileList.Add(dstFile);
                }

                foreach (string fileRelPath in addedFiles)
                {
                    if (fileRelPath == "ProjectMetaData.bin") continue; 
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Added");
                    string? fileHash = HashTool.GetFileMD5CheckSum(mainProject.ProjectPath, fileRelPath);
                    ProjectFile file = new ProjectFile(mainProject.ProjectPath, fileRelPath, fileHash, DataState.Added | DataState.IntegrityChecked, ProjectDataType.Directory);
                    fileChanges.Add(new ChangedFile(file, DataState.Added | DataState.IntegrityChecked));
                }

                foreach (string fileRelPath in deletedFiles)
                {
                    fileIntegrityLog.AppendLine($"{fileRelPath} has been Deleted");
                    ProjectFile srcFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.None); 
                    ProjectFile dstFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.Deleted | DataState.IntegrityChecked);
                    dstFile.UpdatedTime = DateTime.Now;
                    fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Deleted | DataState.IntegrityChecked));
                    changedFileList.Add(srcFile);
                }

                foreach(string fileRelPath in intersectFiles)
                {
                    string? dirFileHash = HashTool.GetFileMD5CheckSum(mainProject.ProjectPath, fileRelPath);
                    if (projectFilesDict[fileRelPath].DataHash != dirFileHash)
                    {
                        fileIntegrityLog.AppendLine($"File {projectFilesDict[fileRelPath].DataName} on {fileRelPath} has been modified");

                        ProjectFile srcFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.None);
                        ProjectFile dstFile = new ProjectFile(projectFilesDict[fileRelPath], DataState.Modified | DataState.IntegrityChecked);
                        dstFile.DataHash = dirFileHash;
                        dstFile.UpdatedTime = new FileInfo(srcFile.DataAbsPath).LastAccessTime;
                        fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Modified | DataState.IntegrityChecked));
                        registeredFilesDict.Add(dstFile.DataRelPath, dstFile);
                    }
                }

                foreach (ProjectFile detectedFile in registeredFilesDict.Values)
                {
                    changedFileList.Add(new ProjectFile(detectedFile));
                }
                fileIntegrityLog.AppendLine("Integrity Check Complete");
                DataStagedEventHandler?.Invoke(fileChanges);
                DataPreStagedEventHandler?.Invoke(changedFileList);
                IntegrityCheckEventHandler?.Invoke(sender, fileIntegrityLog.ToString(), changedFileList);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run File Integrity Check");
            }
        }

        /// <summary>
        /// Merging Src => Dst, Occurs in version Reversion or Merging from outer source. 
        /// </summary>
        /// <param name="srcData"></param>
        /// <param name="dstData"></param>
        /// <param name="isRevert"> True if Reverting, else (Merge) false.</param>
        public List<ChangedFile>? FindVersionDifferences(ProjectData srcData, ProjectData dstData, bool isProjectRevert)
        {
            if (!isProjectRevert)
                return FindVersionDifferences(srcData, dstData);
            try
            {
                List<ChangedFile> fileChanges = new List<ChangedFile>();

                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();

                Dictionary<string, ProjectFile> srcDict = srcData.ProjectFiles;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFiles;

                // Files which is not on the Dst 
                IEnumerable<string> filesToAdd = srcData.ProjectRelFilePathsList.Except(dstData.ProjectRelFilePathsList);
                // Files which is not on the Src
                IEnumerable<string> filesToDelete = dstData.ProjectRelFilePathsList.Except(srcData.ProjectRelFilePathsList);
                // Directories which is not on the Src
                IEnumerable<string> dirsToAdd = srcData.ProjectRelDirsList.Except(dstData.ProjectRelDirsList);
                // Directories which is not on the Dst
                IEnumerable<string> dirsToDelete = dstData.ProjectRelDirsList.Except(srcData.ProjectRelDirsList);
                // Files to Overwrite
                IEnumerable<string> intersectFiles = srcData.ProjectRelFilePathsList.Intersect(dstData.ProjectRelFilePathsList);

                //1. Directories 
                foreach (string dirRelPath in dirsToAdd)
                {
                    ProjectFile dstFile = new ProjectFile(srcDict[dirRelPath], DataState.Added, dstData.ProjectPath);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Added);
                    fileChanges.Add(newChange);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    ProjectFile dstFile = new ProjectFile(dstDict[dirRelPath], DataState.Deleted);
                    fileChanges.Add(new ChangedFile(dstFile, DataState.Deleted));
                }

                foreach (string fileRelPath in filesToAdd)
                {
                    if (!backupFiles.TryGetValue(srcDict[fileRelPath].DataHash, out ProjectFile? backupFile))
                    {
                        MessageBox.Show($"Following Previous Project Version {srcData.UpdatedVersion}\n" +
                            $"Lacks Backup File {srcDict[fileRelPath].DataName}");
                        return null;
                    }
                    ProjectFile srcFile = new ProjectFile(backupFile, DataState.Backup, backupFile.DataSrcPath);
                    ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Restored);
                    fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Restored));
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Deleted);
                    fileChanges.Add(new ChangedFile(dstFile, DataState.Deleted));
                }

                foreach (string fileRelPath in intersectFiles)
                {
                    if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
                    {
                        if (!backupFiles.TryGetValue(srcDict[fileRelPath].DataHash, out ProjectFile? backupFile))
                        {
                            MessageBox.Show($"Following Previous Project Version {srcData.UpdatedVersion} Lacks Backup File {srcDict[fileRelPath].DataName}");
                            return null;
                        }
                        ProjectFile srcFile = new ProjectFile(backupFile, DataState.Backup, backupFile.DataSrcPath);
                        ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Restored);
                        fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Restored));
                    }
                }
                foreach (ChangedFile changedFile in fileChanges)
                {
                    if (changedFile.DstFile != null)
                    {
                        changedFileList.Add(new ProjectFile(changedFile.DstFile));
                    }
                }
                DataPreStagedEventHandler?.Invoke(changedFileList);
                DataPreStagedEventHandler?.Invoke(fileChanges);
                return fileChanges;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run Find Version Differences For Backup");
                return null;
            }
        }
        public List<ChangedFile>? FindVersionDifferences(ProjectData srcData, ProjectData dstData)
        {
            try
            {
                List<ChangedFile> fileChanges = new List<ChangedFile>();

                List<string> recordedFiles = new List<string>();
                List<string> directoryFiles = new List<string>();

                Dictionary<string, ProjectFile> srcDict = registeredFilesDict;
                Dictionary<string, ProjectFile> dstDict = dstData.ProjectFiles;

                // Files which is not on the Dst 
                IEnumerable<string> filesToAdd = srcData.ProjectRelFilePathsList.Except(dstData.ProjectRelFilePathsList);
                // Files which is not on the Src
                IEnumerable<string> filesToDelete = dstData.ProjectRelFilePathsList.Except(srcData.ProjectRelFilePathsList);
                // Directories which is not on the Src
                IEnumerable<string> dirsToAdd = srcData.ProjectRelDirsList.Except(dstData.ProjectRelDirsList);
                // Directories which is not on the Dst
                IEnumerable<string> dirsToDelete = dstData.ProjectRelDirsList.Except(srcData.ProjectRelDirsList);
                // Files to Overwrite
                IEnumerable<string> intersectFiles = srcData.ProjectRelFilePathsList.Intersect(dstData.ProjectRelFilePathsList);

                //1. Directories 
                foreach (string dirRelPath in dirsToAdd)
                {
                    ProjectFile dstFile = new ProjectFile(srcDict[dirRelPath], DataState.Added, dstData.ProjectPath);
                    ChangedFile newChange = new ChangedFile(dstFile, DataState.Added);
                    fileChanges.Add(newChange);
                }

                foreach (string dirRelPath in dirsToDelete)
                {
                    ProjectFile dstFile = new ProjectFile(dstDict[dirRelPath], DataState.Deleted);
                    fileChanges.Add(new ChangedFile(dstFile, DataState.Deleted));
                }

                foreach (string fileRelPath in filesToAdd)
                {
                    ProjectFile srcFile = new ProjectFile(srcDict[fileRelPath], DataState.None);
                    ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Added);
                    fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Added));
                }

                foreach (string fileRelPath in filesToDelete)
                {
                    ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Deleted);
                    fileChanges.Add(new ChangedFile(dstFile, DataState.Deleted));
                }

                foreach (string fileRelPath in intersectFiles)
                {
                    if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
                    {
                        ProjectFile srcFile = new ProjectFile(srcDict[fileRelPath], DataState.None);
                        ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataState.Modified);
                        fileChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Modified));
                    }
                }
                foreach (ChangedFile changedFile in fileChanges)
                {
                    if (changedFile.DstFile != null)
                    {
                        changedFileList.Add(new ProjectFile(changedFile.DstFile));
                    }
                }
                DataPreStagedEventHandler?.Invoke(changedFileList);
                return fileChanges;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run Find Version Differences Against Given Src");
                return null;
            }
        }
        #endregion
        #region Calls For File Check
        public void RetrieveDataSrc(string srcPath)
        {
            try
            {
                string[] binFiles = Directory.GetFiles(srcPath, "VersionLog.*", SearchOption.AllDirectories);
                if (binFiles.Length == 1)
                {
                    var stream = File.ReadAllBytes(binFiles[0]);
                    ProjectData? srcProjectData = MemoryPackSerializer.Deserialize<ProjectData>(stream);
                    if (srcProjectData != null)
                    {
                        ChangedFileList.Clear();
                        srcProjectData.ProjectPath = srcPath;
                        srcProjectData.SetProjectFilesSrcPath();
                        RegisterNewData(srcProjectData);
                        DataPreStagedEventHandler?.Invoke(ChangedFileList);
                    }
                }
                else
                {
                    RegisterNewData(srcPath);
                }
                DataPreStagedEventHandler?.Invoke(ChangedFileList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File Manager RetrieveDataSrc Error: {ex.Message}");
            }
            
        }
        public void RegisterNewData(string updateDirPath)
        {
            string[]? filesFullPaths;
            string[]? dirsFullPaths;
            try
            {
                filesFullPaths = Directory.GetFiles(updateDirPath, "*", SearchOption.AllDirectories);
                dirsFullPaths = Directory.GetDirectories(updateDirPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"FileManager RegisterNewData Error {ex.Message}");
                filesFullPaths = null;
                dirsFullPaths = null;
            }
            if (filesFullPaths == null || dirsFullPaths == null)
            {
                WPF.MessageBox.Show($"Couldn't get files or dirrectories from given Directory {updateDirPath}");
                return;
            }

            try
            {
                foreach (string fileAbsPath in filesFullPaths)
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        new FileInfo(fileAbsPath).Length,
                        FileVersionInfo.GetVersionInfo(fileAbsPath).FileVersion,
                        Path.GetFileName(fileAbsPath),
                        updateDirPath,
                        Path.GetRelativePath(updateDirPath, fileAbsPath)
                        );
                    changedFileList.Add(newFile);
                    if (!registeredFilesDict.TryAdd(newFile.DataRelPath, newFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {newFile.DataName}: for Update");
                    }
                    else continue; 
                }
                foreach (string dirAbsPath in dirsFullPaths)
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        Path.GetFileName(dirAbsPath),
                        updateDirPath,
                        Path.GetRelativePath(updateDirPath, dirAbsPath)
                        );
                    changedFileList.Add(newFile);
                    if (!registeredFilesDict.TryAdd(newFile.DataRelPath, newFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {newFile.DataName}: for Update");
                    }
                    else continue;
                }
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"FileManager RegisterNewData Error {ex.Message}");
                return;
            }
        }
        public void RegisterNewData(ProjectData srcProjectData)
        {
            try
            {
                foreach (ProjectFile srcFile in srcProjectData.ProjectFiles.Values)
                {
                    srcFile.DataState |= DataState.PreStaged;
                    changedFileList.Add(srcFile);
                    if (!registeredFilesDict.TryAdd(srcFile.DataRelPath, srcFile))
                    {
                        WPF.MessageBox.Show($"Already Enlisted File {srcFile.DataName}: for Update");
                    }
                    else continue;
                }

            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show(ex.Message);
                return;
            }
        }
        
        public async Task StageNewFilesAsync()
        {
            changedFileList.Clear();
            await HashPreStagedFilesAsync();
            UpdateStageFileList();
        }

        public void StageNewFiles(ProjectData srcProjectData)
        {
            if (currentProjectData == null)
            {
                MessageBox.Show("Project Main is Missing for FileManager");
                return;
            }
            changedFileList.Clear();
            List<ChangedFile>? changedFiles = FindVersionDifferences(srcProjectData, currentProjectData, false);
            UpdateStageFileList();
        }

        public void RegisterNewfile(ProjectFile projectFile, DataState fileState)
        {
            ProjectFile newfile = new ProjectFile(projectFile, fileState | DataState.PreStaged);
            if (!registeredFilesDict.TryAdd(newfile.DataRelPath, newfile))
            {
                PreStagedDataOverlapEventHandler?.Invoke(newfile);
                return;
            }
            changedFileList.Add(newfile);
            DataPreStagedEventHandler?.Invoke(changedFileList);
        }
        #endregion
        #region Update File Model
        private void UpdateStageFileList()
        {
            if (registeredFilesDict.Count <= 0) return;
            List<ChangedFile> stagedChanges = new List<ChangedFile>();
            foreach (ProjectFile file in registeredFilesDict.Values)
            {
                file.DataState &= ~DataState.PreStaged;
                if ((file.DataState & DataState.Deleted) != 0)
                {
                    ChangedFile newChange = new ChangedFile(new ProjectFile(file), DataState.Deleted);
                    changedFileList.Add(file);
                }
                //compare the hash value, and if its the same, request to remove that file. 
                if (projectFilesDict.TryGetValue(file.DataRelPath, out var srcProjectFile))
                {
                    if (srcProjectFile.DataHash != file.DataHash)
                    {
                        ProjectFile srcFile = new ProjectFile(srcProjectFile);
                        ProjectFile dstFile = new ProjectFile(file, DataState.Modified);
                        stagedChanges.Add(new ChangedFile(srcFile, dstFile, DataState.Modified));
                        changedFileList.Add(dstFile);
                    }
                    else continue;
                }
                else
                {
                    ProjectFile newFile = new ProjectFile(file, DataState.Added);
                    stagedChanges.Add(new ChangedFile(newFile, DataState.Added));
                    changedFileList.Add(newFile);
                }
            }
            DataStagedEventHandler?.Invoke(stagedChanges);
            DataPreStagedEventHandler?.Invoke(changedFileList);
        }
        private void UpdateStageFileList(List<ChangedFile> changedFiles)
        {
            if (registeredFilesDict.Count <= 0) return;
            foreach (ChangedFile changes in changedFiles)
            {
                if (changes.SrcFile == null) return;
                changes.SrcFile.DataState &= ~DataState.PreStaged;

                if (changes.DstFile == null) return;
                changes.DstFile.DataState &= ~DataState.PreStaged;
            }
            foreach (ProjectFile file in registeredFilesDict.Values)
            {
                file.DataState &= ~DataState.PreStaged;
                if ((file.DataState & DataState.Deleted) != 0)
                {
                    ChangedFile newChange = new ChangedFile(new ProjectFile(file), DataState.Deleted);
                    changedFileList.Add(file);
                }
                //compare the hash value, and if its the same, request to remove that file. 
                if (projectFilesDict.TryGetValue(file.DataRelPath, out var srcProjectFile))
                {
                    if (srcProjectFile.DataHash != file.DataHash)
                    {
                        ProjectFile srcFile = new ProjectFile(srcProjectFile);
                        ProjectFile dstFile = new ProjectFile(file, DataState.Modified);
                        changedFiles.Add(new ChangedFile(srcFile, dstFile, DataState.Modified));
                        changedFileList.Add(dstFile);
                    }
                    else continue;
                }
                else
                {
                    ProjectFile newFile = new ProjectFile(file, DataState.Added);
                    changedFiles.Add(new ChangedFile(newFile, DataState.Added));
                    changedFileList.Add(newFile);
                }
            }

            DataStagedEventHandler?.Invoke(changedFiles);
            DataPreStagedEventHandler?.Invoke(changedFileList);
        }
        #endregion
        #region CallBacks From Parent Model 
        public void ProjectLoadedCallback(object projObj)
        {
            if (projObj is not ProjectData loadedProject) return;

            registeredFilesDict.Clear();
            changedFileList.Clear();
            this.currentProjectData = loadedProject;
            this.projectFilesDict = currentProjectData.ProjectFiles;
        }
        public void MetaDataLoadedCallBack(object metaDataObj)
        {
            if (metaDataObj is not ProjectMetaData projectMetaData) return;
            if (projectMetaData == null) return;
            this.backupFiles = projectMetaData.BackupFiles;
        }
        #endregion
    }
}
#region Deprecated 
//public void FindVersionDifferences(ProjectData srcData, ProjectData dstData, ObservableCollection<ProjectFile> changeList)
//{
//    if (srcData == null || dstData == null)
//    {
//        MessageBox.Show($"One or more project is set to null");
//        return;
//    }
//    try
//    {
//        List<ChangedFile> diffLog = new List<ChangedFile>();
//        StringBuilder fileIntegrityLog = new StringBuilder();
//        fileIntegrityLog.AppendLine($"Conducting Version Integrity Check on {dstData.UpdatedVersion}");
//        Dictionary<string, ProjectFile> srcDict = srcData.ProjectFilesDict;
//        Dictionary<string, ProjectFile> dstDict = dstData.ProjectFilesDict;

//        List<string> recordedFiles = new List<string>();
//        List<string> directoryFiles = new List<string>();

//        // Files which is not on the Dst 
//        IEnumerable<string> filesToAdd = srcData.ProjectRelFilePathsList.Except(dstData.ProjectRelFilePathsList);
//        // Files which is not on the Src
//        IEnumerable<string> filesToDelete = dstData.ProjectRelFilePathsList.Except(srcData.ProjectRelFilePathsList);
//        // Directories which is not on the Src
//        IEnumerable<string> dirsToAdd = srcData.ProjectRelDirsList.Except(dstData.ProjectRelDirsList);
//        // Directories which is not on the Dst
//        IEnumerable<string> dirsToDelete = dstData.ProjectRelDirsList.Except(srcData.ProjectRelDirsList);
//        // Files to Overwrite
//        IEnumerable<string> intersectFiles = srcData.ProjectRelFilePathsList.Intersect(dstData.ProjectRelFilePathsList);

//        foreach (string dirRelPath in dirsToAdd)
//        {
//            ProjectFile srcFile = new ProjectFile(srcDict[dirRelPath]);
//            ProjectFile dstFile = new ProjectFile(srcDict[dirRelPath], DataChangedState.Added, dstData.ProjectPath);
//            ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataChangedState.Added);
//            diffLog.Add(newChange);
//        }

//        foreach (string dirRelPath in dirsToDelete)
//        {
//            ProjectFile dstFile = new ProjectFile(dstDict[dirRelPath], DataChangedState.Deleted);
//            changeList.Add(dstFile);
//        }

//        foreach (string fileRelPath in filesToAdd)
//        {
//            ProjectFile srcFile = new ProjectFile(srcDict[fileRelPath]);
//            ProjectFile dstFile = new ProjectFile(srcDict[fileRelPath], DataChangedState.Added, dstData.ProjectPath);
//            ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataChangedState.Added);
//            diffLog.Add(newChange);
//        }

//        foreach (string fileRelPath in filesToDelete)
//        {
//            ProjectFile dstFile = new ProjectFile(dstDict[fileRelPath], DataChangedState.Deleted);
//            ChangedFile newChange = new ChangedFile(dstFile, DataChangedState.Deleted);
//            diffLog.Add(newChange);
//        }

//        foreach (string fileRelPath in intersectFiles)
//        {
//            if (srcDict[fileRelPath].DataHash != dstDict[fileRelPath].DataHash)
//            {
//                ProjectFile dstFile = new ProjectFile(srcDict[fileRelPath], DataChangedState.Modified);
//                ProjectFile srcFile = new ProjectFile(srcDict[fileRelPath]);
//                ChangedFile newChange = new ChangedFile(srcFile, dstFile, DataChangedState.Modified);
//                dstData.ChangedFiles.Add(newChange);
//            }
//        }

//        fileIntegrityLog.AppendLine("Integrity Check Complete");
//    }
//    catch (Exception ex)
//    {
//        System.Windows.MessageBox.Show($"{ex.Message}. Couldn't Run File Integrity Check");
//    }
//}
#endregion