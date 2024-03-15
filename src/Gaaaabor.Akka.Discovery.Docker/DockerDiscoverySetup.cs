using Akka.Actor.Setup;
using Docker.DotNet.Models;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Gaaaabor.Akka.Discovery.Docker
{
    internal class DockerDiscoverySetup : Setup
    {
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
        public string Endpoint { get; set; }

        internal DockerDiscoverySettings Apply(DockerDiscoverySettings settings)
        {
            if (ContainersListParameters != null)
            {
                settings = settings.WithContainersListParameters(ContainersListParameters);
            }

            if (ContainerFilters != null)
            {
                settings = settings.WithContainerFilters(ContainerFilters.ToImmutableList());
            }

            if (!string.IsNullOrEmpty(NetworkNameFilter))
            {
                settings = settings.WithNetworkNameFilter(NetworkNameFilter);
            }

            if (Ports != null)
            {
                settings = settings.WithPorts(Ports.ToImmutableList());
            }

            if (!string.IsNullOrEmpty(Endpoint))
            {
                settings = settings.WithEndpoint(Endpoint);
            }

            return settings;
        }
    }
}
