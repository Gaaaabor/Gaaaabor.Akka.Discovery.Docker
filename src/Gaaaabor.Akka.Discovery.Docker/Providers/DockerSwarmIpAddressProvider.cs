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
        private static char[] _separator = new[] { '/' };

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
                var tasks = await client.Tasks.ListAsync(cancellationToken);
                var taskDetailsTasks = new List<Task<TaskResponse>>();
                foreach (var task in tasks)
                {
                    var taskDetailsTask = client.Tasks.InspectAsync(task.ID, cancellationToken);
                    taskDetailsTasks.Add(taskDetailsTask);
                }

                // TODO: Ehh... cleanup this
                var taskDetailsResponse = await Task.WhenAll(taskDetailsTasks);

                return GetTaskIpAddresses(taskDetailsResponse);
            }
        }

        private List<string> GetTaskIpAddresses(TaskResponse[] taskResponse)
        {
            return taskResponse
                .Where(taskDetails => taskDetails.Status.State == TaskState.Running && taskDetails.NetworksAttachments != null)
                .SelectMany(GetNetworkAttachmentIpAddresses)
                .ToList();
        }

        private List<string> GetNetworkAttachmentIpAddresses(TaskResponse taskDetails)
        {
            // TODO: Add more filters

            return taskDetails.NetworksAttachments
                .Where(networkAttachment => string.IsNullOrEmpty(DockerDiscoverySettings.NetworkNameFilter) || networkAttachment.Network.Spec.Name.Contains(DockerDiscoverySettings.NetworkNameFilter))
                .SelectMany(x => x.Addresses)
                .Select(address => address.Split(_separator, options: StringSplitOptions.RemoveEmptyEntries)[0])
                .ToList();
        }
    }
}