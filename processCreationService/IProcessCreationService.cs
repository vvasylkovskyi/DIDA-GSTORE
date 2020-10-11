namespace processCreationService
{
    public interface IProcessCreationService
    {
        void StartServer(string args);
        void StartClient(string args);
        void ShutdownAllProcesses();
    }
}