﻿using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using System.Text;
using static System.Windows.Forms.AxHost;

namespace SimpleBinaryVCS.Utils
{
    public static class LogTool
    {
        public static void RegisterUpdate(StringBuilder log, string srcProjectVersion, string dstProjectVersion)
        {
            log.AppendLine($"Updating Project From {srcProjectVersion}: To {dstProjectVersion}");
        }
        public static void RegisterChange(StringBuilder log, DataState state, ProjectFile data)
        {
            log.AppendLine($"{state.ToString()} {data.DataRelPath} at {data.UpdatedTime}");
        }
        public static void RegisterChange(StringBuilder log, DataState state, ProjectFile? srcData, ProjectFile dstData)
        {
            if (srcData == null)
            {
                RegisterChange(log, state, dstData);
            }
            else
            {
                log.AppendLine($"{state.ToString()} : {srcData.DataRelPath} at {srcData.UpdatedTime}");
                log.AppendLine($"From : Build Version: {srcData.BuildVersion} Hash : {srcData.DataHash}");
                log.AppendLine($"To : Build Version: {dstData.BuildVersion} Hash : {dstData.DataHash}");
            }
        }
    }
}
