using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorStatus.Services;

namespace MonitorStatus.Controllers
{
        [ApiController]
        [Route("[controller]")]
        public class MonitorController : ControllerBase
        {
            private readonly IMonitorStatus IMonitorService;

            public MonitorController(IMonitorStatus _IMonitorService)
            {
                IMonitorService = _IMonitorService;
            }

            [HttpGet("Start")]
            public void Start()
            {
                 IMonitorService.StartCheck();
                 IMonitorService.Start();
            }

            [HttpGet("Stop")]
            public void Stop()
            {
                IMonitorService.Stop();
            }
        }

}

