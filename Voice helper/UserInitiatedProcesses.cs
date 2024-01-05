using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voice_helper
{
    public class UserInitiatedProcesses
    {
        public int sessionId;
        public string commandName;
        public string processName;
        public DateTime startTime;

        public UserInitiatedProcesses(int _sessionId, string _commandName, string _processName, DateTime _startTime)
        {
            sessionId = _sessionId;
            commandName = _commandName;
            processName = _processName;
            startTime = _startTime;
        }
    }
}
