using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DeployManager.Model
{
    public class LocalConfigData
    {
        public string? LastOpenedDstPath { get; set; }
        public List<string> ignoreFileExtension { get; set; } = new List<string>();
        public List<string> ignoreFileFull { get; set; } = new List<string>();
        public List<string> ignoreDirectory { get; set; } = new List<string>();

        public LocalConfigData(string? LastOpenedDstPath) 
        {
            this.LastOpenedDstPath = LastOpenedDstPath;
        }
        [JsonConstructor]
        public LocalConfigData(string? LastOpenedDstPath, List<string> ignoreFileExtension, List<string> ignoreFileFull, List<string> ignoreDirectory)
        {
            this.LastOpenedDstPath = LastOpenedDstPath;
            this.ignoreFileExtension = ignoreFileExtension;
            this.ignoreFileFull = ignoreFileFull;
            this.ignoreDirectory = ignoreDirectory;
        }
    }
}