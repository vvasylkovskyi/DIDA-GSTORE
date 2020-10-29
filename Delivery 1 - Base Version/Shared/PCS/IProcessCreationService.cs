using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.PCS
{
    public interface IProcessCreationService
    {
        void StartServer(string args);
        void StartClient(string args);
        void ShutdownAllProcesses();
    }
}
