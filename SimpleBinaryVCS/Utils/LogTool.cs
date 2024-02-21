using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using System.Text;

namespace SimpleBinaryVCS.Utils
{
    public static class LogTool
    {
        public static void RegisterChange(StringBuilder log, DataChangedState state, IProjectData data)
        {
            log.AppendLine($"{state.ToString()} {data.DataName} at {data.UpdatedTime}");
        }
        public static void RegisterChange(StringBuilder log, DataChangedState state, ProjectFile srcData, ProjectFile dstData)
        {
            log.AppendLine($"srcData.ToString() {srcData.DataName} at {srcData.UpdatedTime}");
            log.AppendLine($"From : Build Version: {srcData.BuildVersion} Hash : {srcData.DataHash}");
            log.AppendLine($"To : Build Version: {dstData.BuildVersion} Hash : {dstData.DataHash}");
        }
    }
}
