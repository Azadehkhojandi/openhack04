using System;
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

        public IList<Server> GetServers()
        {
            var result = new List<Server>();

            foreach (var i in Cluster.ListNamespacedService("default").Items)
            {
                var server = new Server();

                if (i.Metadata.Labels.Contains(new KeyValuePair<string, string>("type", "raincraft")))
                {
                    server.Name = i.Metadata.Name;

                    foreach (var o in i.Status?.LoadBalancer?.Ingress)
                    {
                        server.Endpoints.Add(new ServerEndpoint()
                        {
                            Minecraft = $"{o.Ip}:25565",
                            Rcon = $"{o.Ip}:25575",
                        });
                    }

                    result.Add(server);
                }
            }

            return result;
        }

        public Server Create(string name)
        {
            var result = new Server();

            if (Found(name))
                throw new Exception("name already exits");

            var labels = new Dictionary<string, string>()
            {
                { "app", name },
                { "type", "raincraft" }
            };

            var claim = new V1PersistentVolumeClaim()
            {
                ApiVersion = "v1",
                Kind = "PersistentVolumeClaim",
                Metadata = new V1ObjectMeta()
                {
                    Name = name
                },
                Spec = new V1PersistentVolumeClaimSpec()
                {
                    AccessModes = new[] { "ReadWriteMany" },
                    StorageClassName = "minesc2",
                    Resources = new V1ResourceRequirements()
                    {
                        Requests = new Dictionary<string, ResourceQuantity>()
                        {
                            { "storage", new ResourceQuantity("5Gi") }
                        }
                    }
                }
            };

            Cluster.CreateNamespacedPersistentVolumeClaim(claim, "default");

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
                                        ClaimName = name
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

            result.Name = service.Metadata.Name;

            foreach (var i in service.Status.LoadBalancer.Ingress)
            {
                result.Endpoints.Add(new ServerEndpoint()
                {
                    Minecraft = i.Ip,
                    Rcon = "25565"
                });

                result.Endpoints.Add(new ServerEndpoint()
                {
                    Minecraft = i.Ip,
                    Rcon = "25575"
                });
            }

            return result;
        }

        public void Delete(string name)
        {
            if (!Found(name))
                throw new Exception("name not found");

            Cluster.DeleteNamespacedDeployment(new V1DeleteOptions(), name, "default");
            Cluster.DeleteNamespacedPersistentVolumeClaim(new V1DeleteOptions(), name, "default");
            Cluster.DeleteNamespacedService(new V1DeleteOptions(), name, "default");
        }

        private bool Found(string name)
        {
            var result = false;

            foreach (var i in Cluster.ListNamespacedService("default").Items)
            {
                if (i.Metadata.Labels.Contains(new KeyValuePair<string, string>("type", "raincraft")))
                {
                    if (i.Metadata.Name == name)
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }
    }
}