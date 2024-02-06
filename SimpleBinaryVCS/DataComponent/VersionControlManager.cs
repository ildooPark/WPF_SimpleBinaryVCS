using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public Action<object> updateAction;
        public Action<object> revertAction;
        public Action<object> projectLoadAction;
        public Action<object> fetchAction; 
        private ProjectData? projectData; 
        public ProjectData ProjectData 
        { 
            get => projectData ?? new ProjectData();
            set
            {
                projectData = value;
            }
        }
        public VersionControlManager()
        {
            projectData = new ProjectData();
        }

        /// <summary>
        /// Returns true if content is the same. 
        /// </summary>
        /// <param name="srcFile"></param>
        /// <param name="dstFile"></param>
        /// <param name="result">First is srcHash, Second is dstHash</param>
        /// <returns></returns>
        public static bool TryCompareMD5CheckSum(string? srcFile, string? dstFile, out (string?, string?) result)
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

        public string? GetMD5CheckSum(string srcFile)
        {
            byte[] srcHashBytes;
            using MD5 md5 = MD5.Create();
            if (md5 == null)
            {
                MessageBox.Show("Failed to Initialize MD5");
                return null; 
            }
            using (var srcStream = File.OpenRead(srcFile))
            {
                srcHashBytes = md5.ComputeHash(srcStream);
            }
            md5.Dispose(); 
            return BitConverter.ToString(srcHashBytes).Replace("-", "");
        }
    }
}