using k8s;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace MineApi
{
    public class DeleteServer
    {
        [FunctionName("DeleteServer")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile($@"{context.FunctionAppDirectory}\config\config.txt");

            new Minecraft(config).Delete(req.Query["server"]);

            return new OkObjectResult(true) as ActionResult;
        }
    }
}