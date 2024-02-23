using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using System.IO;

namespace SimpleBinaryVCS.Utils
{
    public class FileHandlerTool
    {
        public void ApplyFileChanges(List<ChangedFile> Changes)
        {
            if (Changes == null) return;
            foreach (ChangedFile file in Changes)
            {
                if ((file.DataState & DataState.IntegrityChecked) != 0) continue; 
                HandleData(file.SrcFile, file.DstFile, file.DataState);
            }
        }

        public void HandleData(IProjectData dstData, DataState state)
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
        public void HandleData(IProjectData? srcData, IProjectData dstData, DataState state)
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
        public void HandleData(string? srcPath, string dstPath, ProjectDataType type, DataState state)
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
        public void HandleData(string? srcPath, IProjectData dstData, DataState state)
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
        public void HandleDirectory(string? srcPath, string dstPath, DataState state)
        {
            try
            {
                if ((state & DataState.Deleted) != 0)
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
        public void HandleFile(string? srcPath, string dstPath, DataState state)
        {
            try
            {
                if ((state & DataState.Deleted) != 0)
                {
                    if (File.Exists(dstPath))
                        File.Delete(dstPath);
                    return;
                }
                if (srcPath == null) throw new ArgumentNullException(nameof(srcPath));
                else if ((state & DataState.Added) != 0)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(dstPath))) 
                        Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
                    if (!File.Exists(dstPath))
                        File.Copy(srcPath, dstPath, true);
                }
                else
                {
                    if (!Directory.Exists(Path.GetDirectoryName(dstPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
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