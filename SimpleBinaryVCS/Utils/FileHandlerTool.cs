using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using System.IO;

namespace SimpleBinaryVCS.Utils
{
    public class FileHandlerTool
    {
        public void ApplyFileChanges(List<ChangedFile> ChangedFile)
        {
            foreach (ChangedFile file in ChangedFile)
            {
                HandleData(file.SrcFile, file.DstFile, file.DataState);
            }
        }

        public void HandleData(IProjectData dstData, DataChangedState state)
        {
            if (dstData.DataType == ProjectDataType.File)
            {
                HandleFile(null, dstData.DataAbsPath, state);
            }
            else
            {
                HandleDirectory(null, dstData.DataAbsPath, state);
            }
        }
        public void HandleData(IProjectData? srcData, IProjectData dstData, DataChangedState state)
        {
            if (dstData.DataType == ProjectDataType.File)
            {
                HandleFile(srcData?.DataAbsPath, dstData.DataAbsPath, state);
            }
            else
            {
                HandleDirectory(srcData?.DataAbsPath, dstData.DataAbsPath, state);
            }
        }
        public void HandleData(string? srcPath, string dstPath, ProjectDataType type, DataChangedState state)
        {
            if (type == ProjectDataType.File)
            {
                HandleFile(srcPath, dstPath, state);
            }
            else
            {
                HandleDirectory(srcPath, dstPath, state);
            }
        }
        public void HandleData(string? srcPath, IProjectData dstData, DataChangedState state)
        {
            if (dstData.DataType == ProjectDataType.File)
            {
                HandleFile(srcPath, dstData.DataAbsPath, state);
            }
            else
            {
                HandleDirectory(srcPath, dstData.DataAbsPath, state);
            }
        }
        public void HandleDirectory(string? srcPath, string dstPath, DataChangedState state)
        {
            try
            {
                if ((state & DataChangedState.Deleted) != 0)
                {
                    if (Directory.Exists(dstPath))
                        Directory.Delete(dstPath, true);
                }
                else
                {
                    if (!Directory.Exists(dstPath))
                        Directory.CreateDirectory(dstPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void HandleFile(string? srcPath, string dstPath, DataChangedState state)
        {
            try
            {
                if ((state & DataChangedState.Deleted) != 0)
                {
                    if (File.Exists(dstPath))
                        File.Delete(dstPath);
                }
                else
                {
                    if (srcPath == null) throw new ArgumentNullException(nameof(srcPath));
                    File.Copy(srcPath, dstPath, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}