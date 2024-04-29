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
    public class DockerSwarmIpAddressProvider : IpAddressProviderBase
    {
        private static readonly char[] _separator = new[] { '/' };

        public DockerSwarmIpAddressProvider(DockerDiscoverySettings dockerDiscoverySettings, ILoggingAdapter logger) : base(dockerDiscoverySettings, logger)
        { }

        public override async Task<List<IPAddress>> GetIpAddressesAsync(CancellationToken cancellationToken)
        {
            var addresses = new List<IPAddress>();

            try
            {
                Logger.Info("[DockerServiceDiscovery] Getting addresses of Docker services");

                var rawAddresses = new List<string>();

                Logger.Info("[DockerServiceDiscovery] Using swarm mode");

                var swarmAddresses = await GetTaskAddressesAsync(DockerClientConfiguration, cancellationToken);
                if (swarmAddresses.Count > 0)
                {
                    rawAddresses.AddRange(swarmAddresses);
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

        private async Task<List<string>> GetTaskAddressesAsync(DockerClientConfiguration dockerClientConfiguration, CancellationToken cancellationToken)
        {
            using (var client = dockerClientConfiguration.CreateClient())
            {
                var tasksListParameters = DockerDiscoverySettings.TasksListParameters ?? new TasksListParameters();
                var tasks = await client.Tasks.ListAsync(tasksListParameters, cancellationToken);
                var taskDetailsTasks = tasks.Select(task =>
                {
                    return client.Tasks
                        .InspectAsync(task.ID, cancellationToken)
                        .ContinueWith(x => GetNetworkAttachmentIpAddresses(x.Result));
                });

                var taskDetailsResponse = await Task.WhenAll(taskDetailsTasks);
                return taskDetailsResponse.SelectMany(ipAddress => ipAddress).ToList();
            }
        }

        private List<string> GetNetworkAttachmentIpAddresses(TaskResponse taskDetails)
        {
            if (taskDetails.Status.State != TaskState.Running || taskDetails.NetworksAttachments is null)
            {
                return new List<string>();
            }

            // TODO: Add more filters

            IEnumerable<NetworkAttachment> networksAttachmentsQuery = string.IsNullOrWhiteSpace(DockerDiscoverySettings.NetworkNameFilter)
                ? taskDetails.NetworksAttachments
                : taskDetails.NetworksAttachments.Where(networkAttachment => networkAttachment.Network.Spec.Name.Contains(DockerDiscoverySettings.NetworkNameFilter));

            return networksAttachmentsQuery
                .SelectMany(x => x.Addresses)
                .Select(address => address.Split(_separator, options: StringSplitOptions.RemoveEmptyEntries)[0])
                .ToList();
        }
    }
}