using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorStatus.Services;
using MonitorStatus.Entities;

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

            [HttpPost("AddTopic")]
            public Task<OperationResult> AddNewTopic([FromBody] ChannelDTO request)
            {
                return IMonitorService.Subscribe(request.uavNumber.ToString());
            }


            [HttpGet("Stop")]
            public void Stop()
            {
                IMonitorService.Stop();
            }
        

       
    }

}

