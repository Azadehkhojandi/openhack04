using System;
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
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "servers/{name}")] HttpRequest req, TraceWriter log, ExecutionContext context, string name)
        {
            IActionResult result = null;

            try
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile($@"{context.FunctionAppDirectory}\config\config.txt");

                var address = new Minecraft(config).Create(name);

                result = new ObjectResult(address);

                ((ObjectResult)result).StatusCode = 201;
            }
            catch (Exception e)
            {
                result = new OkObjectResult(new { name, error = e.Message });
            }

            return result as ActionResult;
        }
    }
}