using Akka.Actor.Setup;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Gaaaabor.Akka.Discovery.Docker
{
    internal class DockerDiscoverySetup : Setup
    {
        /// <summary>
        /// Additional filtering rules to be applied to Containers
        /// </summary>
        public List<Filter> ContainerFilters { get; set; } = new List<Filter>();

        /// <summary>
        /// Additional filtering rules to be applied to Networks
        /// </summary>
        public string NetworkNameFilter { get; set; }

        /// <summary>
        /// List of ports to be considered as Akka.Management ports on each instance.
        /// </summary>
        public List<int> Ports { get; set; } = new List<int>();

        /// <summary>        
        /// Client may use specified endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        internal DockerDiscoverySettings Apply(DockerDiscoverySettings settings)
        {
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
