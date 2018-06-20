using System.Collections.Generic;

namespace MineApi
{
    public class ServerEndpoint
    {
        public string Minecraft { get; set; }

        public string Rcon { get; set; }
    }

    public class Server
    {
        public string Name { get; set; }

        public List<ServerEndpoint> Endpoints { get; set; }

        public Server()
        {
            Endpoints = new List<ServerEndpoint>();
        }
    }
}