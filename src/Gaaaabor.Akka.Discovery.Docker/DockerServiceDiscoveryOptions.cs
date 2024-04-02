using Akka.Actor.Setup;
using Akka.Hosting;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Gaaaabor.Akka.Discovery.Docker
{
    public class DockerServiceDiscoveryOptions : IHoconOption
    {
        private const string FullPath = "akka.discovery.docker";

        public string ConfigPath { get; } = "docker";

        public Type Class { get; } = typeof(DockerServiceDiscovery);

        /// <summary>
        /// Filtering rules for the Docker API itself (API based filtering).
        /// For more info see <see href="https://github.com/dotnet/Docker.DotNet/issues/303"/>
        /// </summary>
        public ContainersListParameters ContainersListParameters { get; set; }

        /// <summary>
        /// Additional filtering rules to be applied to ContainerListResponse (API result filtering).
        /// Filterable properties of <see cref="ContainerListResponse"/>:
        /// <list type="bullet">
        /// <item>ID: new Filter("ID", "aa58b5ac20cbcc9d76761b08aec92189b68b29a10bfebbb0b91c1cdd3dbeec73")</item>
        /// <item>Image: new Filter("Image", "gaaaaborakkadiscoverydocker-weather-example")</item>
        /// <item>ImageID: new Filter("ImageID", "sha256:639dd40c7818113346626d682c0015f366f3574a0f9056d4cfef497ded23e5b1")</item>
        /// <item>State: new Filter("State", "running")</item>
        /// <item>Names: new Filter("Names", "weather-example")</item>
        /// <item>Labels: new Filter("Labels", "com.docker.compose.service:weather-example")</item>
        /// </list>
        /// </summary>
        public List<Filter> ContainerFilters { get; set; } = new List<Filter>();

        /// <summary>
        /// Additional filtering rules to be applied to Networks (API result filtering).
        /// </summary>
        public string NetworkNameFilter { get; set; }

        /// <summary>
        /// List of ports to be considered as Akka.Management ports on each instance.
        /// </summary>
        public List<int> Ports { get; set; } = new List<int>();

        /// <summary>        
        /// Docker API endpoint.
        /// </summary>
        public string Endpoint { get; set; } = "unix:///var/run/docker.sock";

        /// <summary>
        /// Indicates if the service discovery should look up in swarm nodes
        /// </summary>
        public bool UseSwarm { get; set; }

        /// <summary>
        /// Builds the HOCON config
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="setup"></param>
        public void Apply(AkkaConfigurationBuilder builder, Setup setup = null)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{FullPath} {{");
            stringBuilder.AppendLine($"class = {Class.AssemblyQualifiedName.ToHocon()}");

            if (ContainersListParameters != null)
            {
                stringBuilder.AppendLine($"containerslistparameters = {JsonSerializer.Serialize(ContainersListParameters).ToHocon()}");
            }

            if (ContainerFilters != null)
            {
                var filters = ContainerFilters
                    .SelectMany(filter => filter.Values.Select(value => (filter.Name, Tag: value)))
                    .Select(t => $"{t.Name}={t.Tag}");

                stringBuilder.AppendLine($"containerfilters = {string.Join(";", filters).ToHocon()}");
            }

            if (!string.IsNullOrEmpty(NetworkNameFilter))
            {
                stringBuilder.AppendLine($"networknamefilter = {NetworkNameFilter.ToHocon()}");
            }

            if (Ports != null)
            {
                stringBuilder.AppendLine($"ports = [{string.Join(",", Ports)}]");
            }

            if (!string.IsNullOrEmpty(Endpoint))
            {
                stringBuilder.AppendLine($"endpoint = {Endpoint.ToHocon()}");
            }

            stringBuilder.AppendLine("}");

            builder.AddHocon(stringBuilder.ToString(), HoconAddMode.Prepend);
        }
    }
}
