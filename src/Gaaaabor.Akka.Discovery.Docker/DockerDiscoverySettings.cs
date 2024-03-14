using Akka.Actor;
using Akka.Configuration;
using System.Collections.Immutable;

namespace Gaaaabor.Akka.Discovery.Docker
{
    public class DockerDiscoverySettings
    {
        public static readonly DockerDiscoverySettings Empty = new DockerDiscoverySettings(
            containerFilters: ImmutableList<Filter>.Empty,
            networkNameFilter: null,
            ports: ImmutableList<int>.Empty,
            endpoint: null);

        public static DockerDiscoverySettings Create(ActorSystem system) => Create(system.Settings.Config.GetConfig("akka.discovery.docker"));

        public static DockerDiscoverySettings Create(Config config)
        {
            return new DockerDiscoverySettings(
                DockerServiceDiscovery.ParseFiltersString(config.GetString("containerfilters")),
                config.GetString("networknamefilter"),
                config.GetIntList("ports").ToImmutableList(),
                config.GetString("endpoint")
            );
        }

        public DockerDiscoverySettings(
            ImmutableList<Filter> containerFilters,
            string networkNameFilter,
            ImmutableList<int> ports,
            string endpoint)
        {
            ContainerFilters = containerFilters;
            NetworkNameFilter = networkNameFilter;
            Ports = ports;
            Endpoint = endpoint;
        }

        public ImmutableList<Filter> ContainerFilters { get; }
        public string NetworkNameFilter { get; }
        public ImmutableList<int> Ports { get; }
        public string Endpoint { get; }

        public DockerDiscoverySettings WithContainerFilters(ImmutableList<Filter> containerFilters) => Copy(containerFilters: containerFilters);

        public DockerDiscoverySettings WithNetworkNameFilter(string networkNameFilter) => Copy(networkNameFilter: networkNameFilter);

        public DockerDiscoverySettings WithPorts(ImmutableList<int> ports) => Copy(ports: ports);

        public DockerDiscoverySettings WithEndpoint(string endpoint) => Copy(endpoint: endpoint);

        private DockerDiscoverySettings Copy(
            ImmutableList<Filter> containerFilters = null,
            string networkNameFilter = null,
            ImmutableList<int> ports = null,
            string endpoint = null)
            => new DockerDiscoverySettings(
                containerFilters: containerFilters ?? ContainerFilters,
                networkNameFilter: networkNameFilter ?? NetworkNameFilter,
                ports: ports ?? Ports,
                endpoint: endpoint ?? Endpoint);
    }
}
