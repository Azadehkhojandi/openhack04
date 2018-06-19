using System;
using System.Collections.Generic;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace openhack04
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            log.Info("C# HTTP trigger function processed a request.");

            //var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            var configpath = $"{context.FunctionAppDirectory}\\config\\config.txt";
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeconfigPath: configpath);

            Console.WriteLine("Starting Request!");

            IKubernetes client = new Kubernetes(config);

            var labels = new Dictionary<string, string>();
            labels.Add("app", "raincraft");

            var ports = new List<V1ContainerPort>();
            ports.Add(new V1ContainerPort() { ContainerPort = 25565 });
            ports.Add(new V1ContainerPort() { ContainerPort = 25575 });

            var env = new List<V1EnvVar>();
            env.Add(new V1EnvVar() { Name = "EULA", Value = "true" });

            var mounts = new List<V1VolumeMount>();
            mounts.Add(new V1VolumeMount() { Name = "minedb", MountPath = "/data" });

            var volumes = new List<V1Volume>();
            volumes.Add(new V1Volume()
            {
                Name = "minedb",
                PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource()
                {
                    ClaimName = "minevol2"
                }
            });

            var spec = new V1PodSpec();
            spec.Containers = new List<V1Container>();
            spec.Containers.Add(new V1Container()
            {
                Name = "raincraft-pod",
                Image = "openhack/minecraft-server:2.0-alpine",
                Ports = ports,
                Env = env,
                VolumeMounts = mounts
            });
            spec.Volumes = volumes;

            var template = new V1PodTemplateSpec()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name = "raincraft-pod",
                    Labels = labels
                },
                Spec = spec
            };

            var deployment = new V1Deployment()
            {
                ApiVersion = "apps/v1",
                Kind = "Deployment",
                Metadata = new V1ObjectMeta()
                {
                    Name = "raincraft",
                },
                Spec = new V1DeploymentSpec()
                {
                    Replicas = 1,
                    Template = template,
                    Selector = new V1LabelSelector()
                    {
                        MatchLabels = labels
                    }
                }
            };

            deployment.Validate();

            var dresult = client.CreateNamespacedDeployment(deployment, "default");

            var list = new List<string>();

            //foreach (var i in client.ListNamespacedService("default").Items)
            //{
            //    if (i.Metadata.Labels.Contains(new KeyValuePair<string, string>("app", "minepod-service")))
            //        list.Add(i.Metadata.Name);
            //}

            var pods = client.ListNamespacedPod("default").Items;

            foreach (var i in pods)
            {
                if (i.Metadata.Labels.Contains(new KeyValuePair<string, string>("app", "raincraft")))
                    list.Add(i.Metadata.Name);
            }

            return new OkObjectResult(list) as ActionResult;
        }
    }
}