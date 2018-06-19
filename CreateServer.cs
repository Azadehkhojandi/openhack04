using k8s;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace MineApi
{
    public static class CreateServer
    {
        [FunctionName("CreateServer")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile($@"{context.FunctionAppDirectory}\config\config.txt");

            var address = new Minecraft(config).Create(req.Query["server"]);

            return new OkObjectResult(address) as ActionResult;
        }
    }
}