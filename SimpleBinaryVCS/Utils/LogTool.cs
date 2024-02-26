using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using System.Text;

namespace SimpleBinaryVCS.Utils
{
    public static class LogTool
    {
        public static void RegisterChange(StringBuilder log, DataState state, ProjectFile data)
        {
            log.AppendLine($"{state.ToString()} {data.DataName} at {data.UpdatedTime}");
        }
        public static void RegisterChange(StringBuilder log, DataState state, ProjectFile? srcData, ProjectFile dstData)
        {
            if (srcData == null)
            {
                RegisterChange(log, state, dstData);
            }
            else
            {
                log.AppendLine($"{srcData.DataName} at {srcData.UpdatedTime}");
                log.AppendLine($"From : Build Version: {srcData.BuildVersion} Hash : {srcData.DataHash}");
                log.AppendLine($"To : Build Version: {dstData.BuildVersion} Hash : {dstData.DataHash}");
            }
        }
    }
}
