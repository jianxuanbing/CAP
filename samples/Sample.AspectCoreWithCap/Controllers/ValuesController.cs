using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.AspectCoreWithCap.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ICapPublisher _publisher;

        private readonly IServiceProvider _serviceProvider;

        private readonly IProcessingServer _processingServer;

        public ValuesController(ICapPublisher publisher, IServiceProvider serviceProvider, IProcessingServer processingServer)
        {
            _publisher = publisher;
            _serviceProvider = serviceProvider;
            _processingServer = processingServer;
        }

        /// <summary>
        /// /api/values
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestGetAsync()
        {
            var test = _serviceProvider.GetService<ITestEventHandler>();
            await _publisher.PublishAsync("test", "隔壁老汪");
            return Ok();
        }

        [HttpGet("testDisposed")]
        public async Task<IActionResult> TestDisposed()
        {
            //var temp = ServiceLocator.Instance.GetService<IProcessingServer>();
            //temp.Dispose();
            _processingServer.Dispose();
            return Ok();
        }
    }
}