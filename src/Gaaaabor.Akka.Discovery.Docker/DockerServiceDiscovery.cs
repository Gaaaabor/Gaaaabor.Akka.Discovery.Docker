using Akka.Actor;
using Akka.Configuration;
using Akka.Discovery;
using Akka.Event;
using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Gaaaabor.Akka.Discovery.Docker
{
    public sealed class DockerServiceDiscovery : ServiceDiscovery
    {
        private readonly ExtendedActorSystem _system;
        private readonly ILoggingAdapter _logger;
        private readonly DockerDiscoverySettings _dockerDiscoverySettings;

        public DockerServiceDiscovery(ExtendedActorSystem system)
        {
            _logger = Logging.GetLogger(system, typeof(DockerServiceDiscovery));
            _system = system;

            _dockerDiscoverySettings = DockerDiscoverySettings.Create(system.Settings.Config.GetConfig("akka.discovery.docker"));
        }

        public override async Task<Resolved> Lookup(Lookup lookup, TimeSpan resolveTimeout)
        {
            var addresses = await GetAddressesAsync();

            var resolvedTargets = new List<ResolvedTarget>();
            if (_dockerDiscoverySettings?.Ports is null || !_dockerDiscoverySettings.Ports.Any())
            {
                return new Resolved(lookup.ServiceName, resolvedTargets);
            }

            foreach (var address in addresses)
            {
                foreach (var port in _dockerDiscoverySettings.Ports)
                {
                    resolvedTargets.Add(new ResolvedTarget(host: address.ToString(), port: port, address: address));
                }
            }

            return new Resolved(lookup.ServiceName, resolvedTargets);
        }

        private async Task<IEnumerable<IPAddress>> GetAddressesAsync()
        {
            var addresses = new List<IPAddress>();

            try
            {
                _logger.Info("[DockerServiceDiscovery] Getting addresses of Docker services...");

                var endpoint = _dockerDiscoverySettings.Endpoint;
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    throw new Exception("Endpoint cannot be null or empty!");
                }

                var dockerClientConfiguration = new DockerClientConfiguration(new Uri(endpoint));

                var containersListParameters = new ContainersListParameters
                {
                    All = true,
                    Filters = new Dictionary<string, IDictionary<string, bool>>()
                };

                if (_dockerDiscoverySettings.ContainerFilters?.Any() is true)
                {
                    containersListParameters.Filters = GetContainerFilters();
                }

                IList<ContainerListResponse> containers;
                using (var client = dockerClientConfiguration.CreateClient())
                {
                    containers = await client.Containers.ListContainersAsync(containersListParameters);
                }

                if (containers == null)
                {
                    return addresses;
                }

                var rawIpAddresses = new List<string>();
                foreach (var container in containers)
                {
                    if (container.NetworkSettings == null || container.NetworkSettings.Networks == null)
                    {
                        continue;
                    }

                    foreach (var network in container.NetworkSettings.Networks)
                    {
                        if (string.IsNullOrEmpty(_dockerDiscoverySettings.NetworkNameFilter) || network.Key.Contains(_dockerDiscoverySettings.NetworkNameFilter))
                        {
                            rawIpAddresses.Add(network.Value.IPAddress);
                        }
                    }
                }

                _logger.Info("[DockerServiceDiscovery] Found services: {0}", rawIpAddresses?.Count ?? 0);

                if (rawIpAddresses is null || rawIpAddresses.Count == 0)
                {
                    return addresses;
                }

                foreach (var rawIpAddress in rawIpAddresses)
                {
                    _logger.Info("[DockerServiceDiscovery] Found address: {0}", rawIpAddress);
                    if (IPAddress.TryParse(rawIpAddress, out var ipAddress))
                    {
                        addresses.Add(ipAddress);
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[DockerServiceDiscovery] Error during {0}.{1}: {2}", nameof(DockerServiceDiscovery), nameof(GetAddressesAsync), ex.Message);
            }

            return addresses;
        }

        private Dictionary<string, IDictionary<string, bool>> GetContainerFilters()
        {
            var containerFilters = new Dictionary<string, IDictionary<string, bool>>();

            foreach (var containerFilter in _dockerDiscoverySettings.ContainerFilters)
            {
                if (!containerFilters.TryGetValue(containerFilter.Name, out var filters))
                {
                    filters = new Dictionary<string, bool>();
                    containerFilters.Add(containerFilter.Name, filters);
                }

                foreach (var filter in containerFilter.Values.Select(x => new KeyValuePair<string, bool>(x, true)))
                {
                    filters.Add(filter);
                }
            }

            return containerFilters;
        }

        internal static ImmutableList<Filter> ParseFiltersString(string filtersString)
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
    }
}
