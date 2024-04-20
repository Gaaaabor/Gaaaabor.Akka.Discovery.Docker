using Akka.Event;
using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Gaaaabor.Akka.Discovery.Docker.Providers
{
    public class DockerIpAddressProvider : IpAddressProviderBase
    {
        public DockerIpAddressProvider(DockerDiscoverySettings dockerDiscoverySettings, ILoggingAdapter logger) : base(dockerDiscoverySettings, logger)
        { }

        public override async Task<List<IPAddress>> GetIpAddressesAsync(CancellationToken cancellationToken)
        {
            var addresses = new List<IPAddress>();

            try
            {
                Logger.Info("[DockerServiceDiscovery] Getting addresses of Docker services");

                var rawAddresses = new List<string>();

                Logger.Info("[DockerServiceDiscovery] Using non-swarm mode");

                var containerAddresses = await GetContainerAddressesAsync(DockerClientConfiguration, cancellationToken);
                if (containerAddresses.Count > 0)
                {
                    rawAddresses.AddRange(containerAddresses);
                }

                Logger.Info("[DockerServiceDiscovery] Found services: {0}", rawAddresses?.Count ?? 0);

                if (rawAddresses is null || rawAddresses.Count == 0)
                {
                    return addresses;
                }

                foreach (var rawAddress in rawAddresses)
                {
                    Logger.Info("[DockerServiceDiscovery] Found address: {0}", rawAddress);
                    if (IPAddress.TryParse(rawAddress, out var ipAddress))
                    {
                        addresses.Add(ipAddress);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[DockerServiceDiscovery] Error during {0}.{1}: {2}", nameof(DockerServiceDiscovery), nameof(GetIpAddressesAsync), ex.Message);
            }

            return addresses;
        }

        private async Task<List<string>> GetContainerAddressesAsync(DockerClientConfiguration dockerClientConfiguration, CancellationToken cancellationToken)
        {
            var rawAddresses = new List<string>();
            IList<ContainerListResponse> containers;

            using (var client = dockerClientConfiguration.CreateClient())
            {
                var containersListParameters = DockerDiscoverySettings.ContainersListParameters ?? new ContainersListParameters();
                containers = await client.Containers.ListContainersAsync(containersListParameters, cancellationToken);
            }

            containers = ApplyContainerFilters(containers).ToList();

            foreach (var container in containers)
            {
                if (container.NetworkSettings is null || container.NetworkSettings.Networks is null)
                {
                    if (container.Ports != null)
                    {
                        rawAddresses.AddRange(container.Ports.Where(x => !string.IsNullOrEmpty(x.IP)).Select(x => x.IP));
                    }

                    continue;
                }

                foreach (var network in container.NetworkSettings.Networks)
                {
                    if (string.IsNullOrEmpty(DockerDiscoverySettings.NetworkNameFilter) || network.Key.Contains(DockerDiscoverySettings.NetworkNameFilter))
                    {
                        rawAddresses.Add(network.Value.IPAddress);
                    }
                }
            }

            return rawAddresses;
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
            foreach (var containerFilter in DockerDiscoverySettings.ContainerFilters)
            {
                var expression = ExpressionCache[containerFilter.Name];
                if (!expression(containerFilter, container))
                {
                    return false;
                }
            }

            return true;
        }
    }
}