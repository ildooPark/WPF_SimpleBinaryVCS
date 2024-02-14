using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBinaryVCS.DataComponent
{
    public class VersionControlManager
    {
        public string? mainProjectPath {  get; set; }
        public Action<object>? updateAction;
        public Action<object>? revertAction;
        public Action<object>? pullAction;
        public Action<object>? projectLoadAction;
        public Action<object>? fetchAction; 
        private ProjectData? projectData; 
        public ProjectData ProjectData 
        { 
            get => projectData ?? new ProjectData();
            set
            {
                projectData = value;
            }
        }
        public ProjectData? NewestProjectData { get; set; }
        public ObservableCollection<ProjectData> projectDataList { get; set; }
        public VersionControlManager()
        {
            projectData = new ProjectData();
            projectDataList = new ObservableCollection<ProjectData>();
        }

        /// <summary>
        /// Returns true if content is the same. 
        /// </summary>
        /// <param name="srcFile"></param>
        /// <param name="dstFile"></param>
        /// <param name="result">First is srcHash, Second is dstHash</param>
        /// <returns></returns>
        public bool TryCompareMD5CheckSum(string? srcFile, string? dstFile, out (string?, string?) result)
        {
            if (srcFile == null || dstFile == null)
            {
                result = (null, null); 
                return false;
            }
            byte[] srcHashBytes, dstHashBytes;
            using MD5 md5 = MD5.Create();
            if (md5 == null)
            {
                MessageBox.Show("Failed to Initialize MD5");
                result = (null, null);  
                return false; 
            }
            using (var srcStream = File.OpenRead(srcFile))
            {
                srcHashBytes = md5.ComputeHash(srcStream);
            }
            using (var dstStream = File.OpenRead(dstFile))
            {
                dstHashBytes = md5.ComputeHash(dstStream);
            }
            string srcHashString = BitConverter.ToString(srcHashBytes).Replace("-", ""); 
            string dstHashString = BitConverter.ToString(srcHashBytes).Replace("-", "");
            result =  (srcHashString, dstHashString);
            return srcHashString == dstHashString;
        }

        public string? GetFileMD5CheckSum(string projectPath, string srcFileRelPath)
        {
            byte[] srcHashBytes;
            string srcFileFullPath = Path.Combine(projectPath, srcFileRelPath);
            using MD5 md5 = MD5.Create();
            if (md5 == null)
            {
                MessageBox.Show("Failed to Initialize MD5");
                return null; 
            }
            using (var srcStream = File.OpenRead(srcFileFullPath))
            {
                srcHashBytes = md5.ComputeHash(srcStream);
            }
            md5.Dispose(); 
            return BitConverter.ToString(srcHashBytes).Replace("-", "");
        }

        public async Task<string?> GetFileMD5CheckSumAsync(ChangedFile targetFile)
        {
            try
            {
                targetFile.fileHashChecked = false; 
                byte[] srcHashBytes;
                using MD5 md5 = MD5.Create();
                if (md5 == null)
                {
                    MessageBox.Show("Failed to Initialize MD5 Async");
                    return null;
                }
                using (var srcStream = File.OpenRead(targetFile.fileFullPath()))
                {
                    srcHashBytes = await md5.ComputeHashAsync(srcStream);
                }
                string resultHash = BitConverter.ToString(srcHashBytes).Replace("-", "");
                md5.Dispose();
                return resultHash;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured {ex.Message} \nwhile Computing hash async by this file {targetFile.fileRelPath}");
                return null;
            }
        }
    }
}