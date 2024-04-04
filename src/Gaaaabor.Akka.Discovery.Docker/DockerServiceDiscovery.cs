using Akka.Actor;
using Akka.Discovery;
using Akka.Event;
using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Gaaaabor.Akka.Discovery.Docker
{
    public sealed class DockerServiceDiscovery : ServiceDiscovery
    {
        private readonly ExtendedActorSystem _system;
        private readonly ILoggingAdapter _logger;
        private readonly DockerDiscoverySettings _dockerDiscoverySettings;
        private readonly Dictionary<string, Func<Filter, ContainerListResponse, bool>> _expressionCache;

        public DockerServiceDiscovery(ExtendedActorSystem system)
        {
            _system = system;
            _logger = Logging.GetLogger(system, typeof(DockerServiceDiscovery));
            _dockerDiscoverySettings = DockerDiscoverySettings.Create(system.Settings.Config.GetConfig("akka.discovery.docker"));
            _expressionCache = new Dictionary<string, Func<Filter, ContainerListResponse, bool>>(StringComparer.OrdinalIgnoreCase);

            BuildSimpleExpressionCache();
        }

        public override async Task<Resolved> Lookup(Lookup lookup, TimeSpan resolveTimeout)
        {
            var cancellationTokenSource = new CancellationTokenSource(resolveTimeout);

            var addresses = await GetAddressesAsync(cancellationTokenSource.Token);

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

        private async Task<IEnumerable<IPAddress>> GetAddressesAsync(CancellationToken cancellationToken)
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

                IList<ContainerListResponse> containers;

                var dockerClientConfiguration = new DockerClientConfiguration(new Uri(endpoint));
                using (var client = dockerClientConfiguration.CreateClient())
                {
                    var containersListParameters = _dockerDiscoverySettings.ContainersListParameters ?? new ContainersListParameters();
                    containers = await client.Containers.ListContainersAsync(containersListParameters, cancellationToken);

                    // TODO: Remove, this is just an early implementation, this should not hit live environment...
                    if (_dockerDiscoverySettings.UseSwarm)
                    {

                    }
                }

                if (containers is null)
                {
                    return addresses;
                }

                var rawIpAddresses = new List<string>();

                containers = ApplyContainerFilters(containers).ToList();

                foreach (var container in containers)
                {
                    if (container.NetworkSettings is null || container.NetworkSettings.Networks is null)
                    {
                        if (container.Ports != null)
                        {
                            rawIpAddresses.AddRange(container.Ports.Where(x => !string.IsNullOrEmpty(x.IP)).Select(x => x.IP));
                        }

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

        private IEnumerable<ContainerListResponse> ApplyContainerFilters(IEnumerable<ContainerListResponse> containers)
        {
            foreach (var container in containers)
            {
                if (IsContainerFiltersMatching(container))
                {
                    yield return container;
                }
            }
        }

        private bool IsContainerFiltersMatching(ContainerListResponse container)
        {
            foreach (var containerFilter in _dockerDiscoverySettings.ContainerFilters)
            {
                var expression = _expressionCache[containerFilter.Name];
                if (!expression(containerFilter, container))
                {
                    return false;
                }
            }

            return true;
        }

        private void BuildSimpleExpressionCache()
        {
            var properties = typeof(ContainerListResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                switch (property.Name)
                {
                    case string name when nameof(ContainerListResponse.ID).Equals(name, StringComparison.OrdinalIgnoreCase):
                        _expressionCache.Add(property.Name, (filter, container) => filter.Values.Any(filterValue => container.ID.Contains(filterValue)));
                        break;

                    case string name when nameof(ContainerListResponse.Image).Equals(name, StringComparison.OrdinalIgnoreCase):
                        _expressionCache.Add(property.Name, (filter, container) => filter.Values.Any(filterValue => container.Image.Contains(filterValue)));
                        break;

                    case string name when nameof(ContainerListResponse.ImageID).Equals(name, StringComparison.OrdinalIgnoreCase):
                        _expressionCache.Add(property.Name, (filter, container) => filter.Values.Any(filterValue => container.ImageID.Contains(filterValue)));
                        break;

                    case string name when nameof(ContainerListResponse.State).Equals(name, StringComparison.OrdinalIgnoreCase):
                        _expressionCache.Add(property.Name, (filter, container) => filter.Values.Any(filterValue => container.State.Contains(filterValue)));
                        break;

                    case string name when nameof(ContainerListResponse.Names).Equals(name, StringComparison.OrdinalIgnoreCase):
                        _expressionCache.Add(property.Name, (filter, container) => filter.Values.Any(filterValue => container.Names.Any(containerName => containerName.Contains(filterValue))));
                        break;

                    case string name when nameof(ContainerListResponse.Labels).Equals(name, StringComparison.OrdinalIgnoreCase):

                        _expressionCache.Add(property.Name, (filter, container) =>
                        {
                            if (filter.Values is null)
                            {
                                return false;
                            }

                            var result = true;
                            foreach (var value in filter.Values)
                            {
                                var split = value.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                                if (split.Length == 2)
                                {
                                    result &= container.Labels.TryGetValue(split[0], out var labelValue) && labelValue.Contains(split[1]);
                                }
                            }

                            return result;
                        });

                        break;

                    default:
                        break;
                }
            }

            _logger.Info("[DockerServiceDiscovery] Simple expression cache successfully built.");
        }
    }
}