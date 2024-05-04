using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployAssistant.Model
{
    [Flags]
    public enum ProjectDataType
    {
        File,
        Directory,
        FileReadOnly
    }
    [Flags]
    public enum DataState
    {
        None = 0,
        Added = 1,
        Deleted = 1 << 1,
        Restored = 1 << 2,
        Modified = 1 << 3,
        PreStaged = 1 << 4,
        IntegrityChecked = 1 << 5,
        Backup = 1 << 6,
        Overlapped = 1 << 7,
        Integrate = 1 << 8
    }
    [Flags]
    public enum IgnoreType
    {
        None = 0,
        Integration = 1,
        IntegrityCheck = 1 << 1,
        Deploy = 1 << 2,
        Initialization = 1 << 3,
        Checkout = 1 << 4,
        All = ~0
    }
    public enum MetaDataState
    {
        IntegrityChecking,
        CleanRestoring,
        Exporting,
        Reverting,
        Processing,
        Retrieving,
        Updating,
        IntegrationValidating,
        Integrating,
        Initializing,
        ProcessingBackup,
        Idle
    }
    [Flags]
    public enum IgnoreFileType
    {
        Directory = 0,
        File = 1,
        Integration = 1 << 1,
        Deploy = 1 << 2
    }
    public enum DeployMode
    {
        Safe,
        Unsafe,
        IntegrityCheck
    }
}
