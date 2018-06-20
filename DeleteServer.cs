using System;
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
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "servers/{name}")] HttpRequest req, TraceWriter log, ExecutionContext context, string name)
        {
            IActionResult result = null;

            try
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile($@"{context.FunctionAppDirectory}\config\config.txt");

                new Minecraft(config).Delete(req.Query["name"]);

                result = new NoContentResult();
            }
            catch (Exception e)
            {
                result = new OkObjectResult(new { name, error = e.Message });
            }

            return result as ActionResult;
        }
    }
}