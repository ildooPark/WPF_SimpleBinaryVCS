using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Interfaces;
using System.IO;

namespace SimpleBinaryVCS.Utils
{
    public class FileHandlerTool
    {
        public void HandleFile(IProjectData fileData, DataChangedState state)
        {
            try
            {
                switch (state)
                {
                    case DataChangedState.Added:
                        break;
                    case DataChangedState.Modified: 
                        break;
                    case DataChangedState.Deleted: 
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void HandleFile(IProjectData srcData, IProjectData dstData, DataChangedState state)
        {
            try
            {
                switch (state)
                {
                    case DataChangedState.Added:
                        break;
                    case DataChangedState.Modified:
                        break;
                    case DataChangedState.Deleted:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void HandleFile(string srcPath, string dstPath, DataChangedState state)
        {
            try
            {
                switch (state)
                {
                    case DataChangedState.Added:
                        break;
                    case DataChangedState.Modified:
                        break;
                    case DataChangedState.Deleted:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void HandleFile(string dstPath, DataChangedState state)
        {
            try
            {
                switch (state)
                {
                    case DataChangedState.Added:
                        break;
                    case DataChangedState.Modified:
                        break;
                    case DataChangedState.Deleted:
                        File.Delete(dstPath);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}