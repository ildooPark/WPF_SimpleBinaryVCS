﻿using MemoryPack;
using Microsoft.VisualBasic.Logging;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace SimpleBinaryVCS.Model
{
    [MemoryPackable]
    public partial class ProjectData : IEquatable<ProjectData>, IComparer<ProjectData>, IComparable<ProjectData>
    {
        public string ProjectName { get; set; }
        public string ProjectPath { get; set; }
        public string UpdaterName { get; set; }
        public string ConductedPC { get; set; }
        public DateTime UpdatedTime { get; set; }
        public string UpdatedVersion { get; set; }
        public string UpdateLog { get; set; }
        public string ChangeLog { get; set; }
        public int RevisionNumber { get; set; } = 0;
        public int NumberOfChanges { get; set; }
        public List<ChangedFile> ChangedFiles {  get; set; } = new List<ChangedFile>();
        public Dictionary<string, ProjectFile> ProjectFiles { get; set; }

        /// <summary>
        /// Key : Data Relative Path 
        /// Value : ProjectFile
        /// </summary>
        [MemoryPackIgnore]
        public ObservableCollection<ProjectFile> ProjectFilesObs => new ObservableCollection<ProjectFile>(ProjectFiles.Values.ToList());
        [MemoryPackIgnore]
        public List<string> ProjectRelDirsList => ProjectFiles.Values.ToList()
            .Where(file => file.DataType == Interfaces.ProjectDataType.Directory)
            .Select(file => file.DataRelPath)
            .ToList();
        [MemoryPackIgnore]
        public List<string> ProjectRelFilePathsList => ProjectFiles.Values.ToList()
            .Where(file => file.DataType == Interfaces.ProjectDataType.File)
            .Select(file => file.DataRelPath)
            .ToList();
        [MemoryPackIgnore]
        public List<ProjectFile> ChangedDstFileList
        {
            get
            {
                List<ProjectFile> changedDstFileList = new List<ProjectFile>();
                foreach (ChangedFile changes in ChangedFiles)
                {
                    if (changes.DstFile != null) changedDstFileList.Add(changes.DstFile);
                }
                return changedDstFileList;
            }
        }
        [MemoryPackIgnore]
        public ObservableCollection<ProjectFile> ChangedProjectFileObservable
        {
            get
            {
                ObservableCollection<ProjectFile> changedFilesObservable = new ObservableCollection<ProjectFile>();
                foreach (ChangedFile changes in ChangedFiles)
                {
                    if (changes.DstFile != null) changedFilesObservable.Add(changes.DstFile);
                    if (changes.SrcFile != null) changedFilesObservable.Add(changes.SrcFile);
                }
                return changedFilesObservable;
            }
        }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProjectData() { }
        [MemoryPackConstructor]
        public ProjectData(string ProjectName, string ProjectPath, string UpdaterName, string ConductedPC, 
            DateTime UpdatedTime, string UpdatedVersion, string UpdateLog, string ChangeLog, 
            int RevisionNumber, int NumberOfChanges, List<ChangedFile> ChangedFiles, Dictionary<string, ProjectFile> ProjectFiles)
        {
            this.ProjectName = ProjectName;
            this.ProjectPath = ProjectPath;
            this.UpdaterName = UpdaterName;
            this.ConductedPC = ConductedPC;
            this.UpdatedTime = UpdatedTime;
            this.UpdatedVersion = UpdatedVersion;
            this.UpdateLog = UpdateLog;
            this.ChangeLog = ChangeLog;
            this.RevisionNumber = RevisionNumber;
            this.NumberOfChanges = NumberOfChanges;
            this.ChangedFiles = ChangedFiles;
            this.ProjectFiles = ProjectFiles;
        }

        public ProjectData(string projectPath)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            this.ProjectPath = projectPath;
            this.RevisionNumber = 0;
            this.ProjectFiles = new Dictionary<string, ProjectFile>();
            this.ChangedFiles = new List<ChangedFile>();
        }

        public ProjectData(ProjectData srcProjectData)
        {
            this.ProjectName = srcProjectData.ProjectName;
            this.ProjectPath = srcProjectData.ProjectPath;
            this.UpdaterName = srcProjectData.UpdaterName;
            this.UpdatedTime = srcProjectData.UpdatedTime;
            this.UpdatedVersion = srcProjectData.UpdatedVersion;
            this.ConductedPC = srcProjectData.ConductedPC;
            this.UpdateLog = srcProjectData.UpdateLog;
            this.ChangeLog = srcProjectData.ChangeLog;
            this.NumberOfChanges = srcProjectData.NumberOfChanges;
            this.RevisionNumber = srcProjectData.RevisionNumber;
            this.ProjectFiles = new Dictionary<string, ProjectFile>(srcProjectData.ProjectFiles);
            this.ChangedFiles = new List<ChangedFile>(srcProjectData.ChangedFiles);
        }

        public ProjectData(ProjectData srcProjectData, string projectPath, string updaterName, 
            DateTime updateTime, string updatedVersion, string conductedPC, string updateLog, string? changeLog,
            Dictionary<string, ProjectFile> projectFiles,
            List<ChangedFile> changedFiles)
        {
            this.ProjectName = srcProjectData.ProjectName;
            this.ProjectPath = projectPath;
            this.UpdaterName = updaterName;
            this.UpdatedTime = updateTime;
            this.UpdatedVersion = updatedVersion;
            this.ConductedPC = conductedPC;
            this.UpdateLog = updateLog;
            this.ChangeLog = changeLog ?? "";
            this.NumberOfChanges = changedFiles.Count;
            this.RevisionNumber = ++srcProjectData.RevisionNumber;
            this.ProjectFiles = new Dictionary<string, ProjectFile>(projectFiles);
            this.ChangedFiles = new List<ChangedFile>(changedFiles);
        }

        public ProjectData(ProjectData srcProjectData, bool IsReverting)
        {
            this.ProjectName = srcProjectData.ProjectName;
            this.ProjectPath = srcProjectData.ProjectPath;
            this.UpdaterName = srcProjectData.UpdaterName;
            this.UpdatedTime = srcProjectData.UpdatedTime;
            this.UpdatedVersion = srcProjectData.UpdatedVersion;
            this.ConductedPC = srcProjectData.ConductedPC;
            this.UpdateLog = srcProjectData.UpdateLog;
            this.ChangeLog = srcProjectData.ChangeLog;
            this.NumberOfChanges = srcProjectData.NumberOfChanges;
            this.RevisionNumber = srcProjectData.RevisionNumber;
            this.ProjectFiles = new Dictionary<string, ProjectFile>(srcProjectData.ProjectFiles);
            if (IsReverting)
            {
                this.ChangedFiles = new List<ChangedFile>(srcProjectData.ChangedFiles);
            }
            else
            {
                this.ChangedFiles = new List<ChangedFile>();
            }
        }

        public bool Equals(ProjectData? other)
        {
            if (other == null) return false;
            return other.UpdatedVersion == this.UpdatedVersion; 
        }

        public int Compare(ProjectData? x, ProjectData? y)
        {
            if (x == null || y == null)
            {
                MessageBox.Show("Invalid Comparison, ProjectData Cannot be Null");
                return 0;
            }
            return x.UpdatedTime.CompareTo(y.UpdatedTime);
        }

        public int CompareTo(ProjectData? other)
        {
            if (other == null)
            {
                MessageBox.Show("Invalid Comparison, ProjectData Cannot be Null");
                return 0;
            }
            return this.UpdatedTime.CompareTo(other.UpdatedTime);
        }

        public void RegisterProjectInfo(Dictionary<string, object> dict)
        {
            dict.Add(nameof(this.ProjectName), this.ProjectName);
            dict.Add(nameof(this.ProjectPath), this.ProjectPath);
            dict.Add(nameof(this.UpdaterName), this.UpdaterName);
            dict.Add(nameof(this.ConductedPC), this.ConductedPC);
            dict.Add(nameof(this.UpdatedTime), this.UpdatedTime);
            dict.Add(nameof(this.UpdatedVersion), this.UpdatedVersion);
            dict.Add(nameof(this.NumberOfChanges), this.NumberOfChanges);
        }

        public void SetProjectFilesSrcPath()
        {
            if (ProjectPath == null)
            {
                MessageBox.Show("Project Path is Null, Couldn't Set Source Data Path for all Project Files");
                return;
            }
            foreach (ProjectFile file in ProjectFilesObs)
            {
                file.DataSrcPath = ProjectPath;
            }
        }
    }
}