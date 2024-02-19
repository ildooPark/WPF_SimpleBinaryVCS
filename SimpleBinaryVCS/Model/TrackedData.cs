using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using System.IO;

namespace SimpleBinaryVCS.Model
{

    public class TrackedData : IProjectData
    {
        private DataChangedState dataChangedState;
        public DataChangedState DataState{ get => dataChangedState; set => dataChangedState = value;}

        private readonly string dataSrcPath;
        public string DataSrcPath => dataSrcPath;


        private readonly string dataRelPath;
        public string DataRelPath => dataRelPath;

        private readonly string dataName;
        public string DataName => dataName;

        public string DataAbsPath => Path.Combine(DataSrcPath, DataRelPath);

        private string? dataHash; 
        public string DataHash { get => dataHash ??= ""; set => dataHash = value; }

        public DateTime ChangedTime { get; set; }
        public ProjectDataType DataType { get; set; }


        /// <summary>
        /// Requires getting fileHash Value. 
        /// </summary>
        /// <param name="dataChangedState"></param>
        /// <param name="dataRelPath"></param>
        /// <param name="dataName"></param>
        /// <param name="dataHash"></param>
        public TrackedData(ProjectDataType dataType, DataChangedState dataChangedState, string dataSrcPath, string dataRelPath, string dataName)
        {
            this.DataType = dataType;
            this.dataChangedState = dataChangedState;
            this.dataSrcPath = dataSrcPath;
            this.dataRelPath = dataRelPath;
            this.dataName = dataName;
            this.ChangedTime = DateTime.Now;
        }
    }
}
