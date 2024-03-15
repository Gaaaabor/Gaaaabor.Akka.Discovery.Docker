using Akka.Actor;
using Akka.Configuration;
using Docker.DotNet.Models;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using AkkaConfig = Akka.Configuration.Config;

namespace Gaaaabor.Akka.Discovery.Docker
{
    public class DockerDiscoverySettings
    {
        public static readonly DockerDiscoverySettings Empty = new DockerDiscoverySettings(
            containersListParameters: null,
            containerFilters: ImmutableList<Filter>.Empty,
            networkNameFilter: null,
            ports: ImmutableList<int>.Empty,
            endpoint: null);

        /// <summary>
        /// Filtering rules for the Docker API itself (API based filtering).
        /// For more info see <see href="https://github.com/dotnet/Docker.DotNet/issues/303"/>
        /// </summary>
        public ContainersListParameters ContainersListParameters { get; }

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
        public ImmutableList<Filter> ContainerFilters { get; }

        /// <summary>
        /// Additional filtering rules to be applied to Networks (API result filtering).
        /// </summary>
        public string NetworkNameFilter { get; }

        /// <summary>
        /// List of ports to be considered as Akka.Management ports on each instance.
        /// </summary>
        public ImmutableList<int> Ports { get; }

        /// <summary>        
        /// Docker API endpoint.
        /// </summary>
        public string Endpoint { get; }

        public DockerDiscoverySettings(
            ContainersListParameters containersListParameters,
            ImmutableList<Filter> containerFilters,
            string networkNameFilter,
            ImmutableList<int> ports,
            string endpoint)
        {
            ContainersListParameters = containersListParameters;
            ContainerFilters = containerFilters;
            NetworkNameFilter = networkNameFilter;
            Ports = ports;
            Endpoint = endpoint;
        }

        public static DockerDiscoverySettings Create(ActorSystem system) => Create(system.Settings.Config.GetConfig("akka.discovery.docker"));

        public static DockerDiscoverySettings Create(AkkaConfig config)
        {
            return new DockerDiscoverySettings(
                ParseContainersListParametersString(config.GetString("containerslistparameters")),
                ParseFiltersString(config.GetString("containerfilters")),
                config.GetString("networknamefilter"),
                config.GetIntList("ports").ToImmutableList(),
                config.GetString("endpoint")
            );
        }

        public DockerDiscoverySettings WithContainersListParameters(ContainersListParameters containersListParameters) => Copy(containersListParameters: containersListParameters);

        public DockerDiscoverySettings WithContainerFilters(ImmutableList<Filter> containerFilters) => Copy(containerFilters: containerFilters);

        public DockerDiscoverySettings WithNetworkNameFilter(string networkNameFilter) => Copy(networkNameFilter: networkNameFilter);

        public DockerDiscoverySettings WithPorts(ImmutableList<int> ports) => Copy(ports: ports);

        public DockerDiscoverySettings WithEndpoint(string endpoint) => Copy(endpoint: endpoint);

        private static ContainersListParameters ParseContainersListParametersString(string containersListParameters)
        {
            return JsonSerializer.Deserialize<ContainersListParameters>(containersListParameters);
        }

        private static ImmutableList<Filter> ParseFiltersString(string filtersString)
        {
            var filters = new List<Filter>();

            if (filtersString is null)
            {
                return filters.ToImmutableList();
            }

            var kvpList = filtersString.Split(';');
            foreach (var kvp in kvpList)
            {
                if (string.IsNullOrEmpty(kvp))
                    continue;

                var pair = kvp.Split('=');
                if (pair.Length != 2)
                    throw new ConfigurationException($"Failed to parse one of the key-value pairs in filters: {kvp}");

                filters.Add(new Filter(pair[0], pair[1].Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList()));
            }

            return filters.ToImmutableList();
        }

        private DockerDiscoverySettings Copy(
            ContainersListParameters containersListParameters = null,
            ImmutableList<Filter> containerFilters = null,
            string networkNameFilter = null,
            ImmutableList<int> ports = null,
            string endpoint = null)
            => new DockerDiscoverySettings(
                containersListParameters: containersListParameters ?? ContainersListParameters,
                containerFilters: containerFilters ?? ContainerFilters,
                networkNameFilter: networkNameFilter ?? NetworkNameFilter,
                ports: ports ?? Ports,
                endpoint: endpoint ?? Endpoint);
    }
}
