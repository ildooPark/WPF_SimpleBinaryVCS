using DeployAssistant.Model;

namespace SimpleBinaryVCS.Interfaces
{
    public interface IManager
    {
        public void Awake();
        event Action<MetaDataState> ManagerStateEventHandler;
    }
}
