using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorStatus.Services
{
    public interface IMonitorStatus
    {
        public Task<OperationResult> Start();
        public Task<OperationResult> Stop();
        public void StartCheck();
    }
}
