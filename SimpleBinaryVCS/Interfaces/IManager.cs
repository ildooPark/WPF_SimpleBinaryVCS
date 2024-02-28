namespace SimpleBinaryVCS.Interfaces
{
    public interface IManager
    {
        public void Awake();
        event Action<string> IssueEventHandler;
    }
}
