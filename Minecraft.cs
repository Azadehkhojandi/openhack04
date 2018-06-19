using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace MineApi
{
    public class Minecraft
    {
        private IKubernetes Cluster { get; set; }

        public Minecraft(KubernetesClientConfiguration config)
        {
            Cluster = new Kubernetes(config);
        }

        public IDictionary<string, IList<string>> GetServers()
        {
            var result = new Dictionary<string, IList<string>>();

            foreach (var i in Cluster.ListNamespacedService("default").Items)
            {
                if (i.Metadata.Labels.Contains(new KeyValuePair<string, string>("type", "raincraft")))
                    result.Add(i.Metadata.Name, i.Spec.ExternalIPs);
            }

            return result;
        }

        public IList<string> Create(string name)
        {
            var labels = new Dictionary<string, string>()
            {
                { "app", name },
                { "type", "raincraft" }
            };

            var deployment = new V1Deployment()
            {
                ApiVersion = "apps/v1",
                Kind = "Deployment",
                Metadata = new V1ObjectMeta()
                {
                    Name = name,
                },
                Spec = new V1DeploymentSpec()
                {
                    Replicas = 1,
                    Template = new V1PodTemplateSpec()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            Name = name,
                            Labels = labels
                        },
                        Spec = new V1PodSpec()
                        {
                            Containers = new List<V1Container>(new[]
                            {
                                new V1Container()
                                {
                                    Name = name,
                                    Image = "openhack/minecraft-server:2.0-alpine",
                                    Ports = new List<V1ContainerPort>(new[]
                                    {
                                        new V1ContainerPort()
                                        {
                                            ContainerPort = 25565
                                        },
                                        new V1ContainerPort()
                                        {
                                            ContainerPort = 25575
                                        },
                                    }),
                                    Env = new List<V1EnvVar>(new[]
                                    {
                                        new V1EnvVar()
                                        {
                                            Name = "EULA",
                                            Value = "true"
                                        }
                                    }),
                                    VolumeMounts = new List<V1VolumeMount>(new[]
                                    {
                                        new V1VolumeMount()
                                        {
                                            Name = "minedb",
                                            MountPath = "/data"
                                        }
                                    })
                                }
                            }),
                            Volumes = new List<V1Volume>(new[]
                            {
                                new V1Volume()
                                {
                                    Name = "minedb",
                                    PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource()
                                    {
                                        ClaimName = "minevol2"
                                    }
                                }
                            })
                        }
                    },
                    Selector = new V1LabelSelector()
                    {
                        MatchLabels = labels
                    }
                }
            };

            deployment = Cluster.CreateNamespacedDeployment(deployment, "default");

            var service = Cluster.CreateNamespacedService(new V1Service()
            {
                ApiVersion = "v1",
                Kind = "Service",
                Metadata = new V1ObjectMeta()
                {
                    Name = name,
                    Labels = labels
                },
                Spec = new V1ServiceSpec()
                {
                    Type = "LoadBalancer",
                    Ports = new List<V1ServicePort>(new[]
                    {
                        new V1ServicePort()
                        {
                            Name = "25565",
                            Protocol = "TCP",
                            Port = 25565,
                            TargetPort = new IntstrIntOrString("25565")
                        },
                        new V1ServicePort()
                        {
                            Name = "25575",
                            Protocol = "TCP",
                            Port = 25575,
                            TargetPort = new IntstrIntOrString("25575")
                        }
                    }),
                    Selector = labels
                }
            }, "default");

            return service.Spec.ExternalIPs;
        }

        public void Delete(string name)
        {
            Cluster.DeleteNamespacedDeployment(new V1DeleteOptions(), name, "default");

            Cluster.DeleteNamespacedService(new V1DeleteOptions(), name, "default");
        }
    }
}