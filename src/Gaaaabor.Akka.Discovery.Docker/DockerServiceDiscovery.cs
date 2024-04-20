using Akka.Actor;
using Akka.Discovery;
using Akka.Event;
using Gaaaabor.Akka.Discovery.Docker.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gaaaabor.Akka.Discovery.Docker
{
    public sealed class DockerServiceDiscovery : ServiceDiscovery
    {
        private readonly ExtendedActorSystem _system;
        private readonly ILoggingAdapter _logger;
        private readonly DockerDiscoverySettings _dockerDiscoverySettings;
        
        private readonly IpAddressProviderBase _ipAddressProvider;

        public DockerServiceDiscovery(ExtendedActorSystem system)
        {
            _system = system;
            _logger = Logging.GetLogger(system, typeof(DockerServiceDiscovery));
            _dockerDiscoverySettings = DockerDiscoverySettings.Create(system.Settings.Config.GetConfig("akka.discovery.docker"));

            _ipAddressProvider = _dockerDiscoverySettings.UseSwarm
                ? new DockerIpAddressProvider(_dockerDiscoverySettings, _logger)
                : (IpAddressProviderBase)new DockerSwarmIpAddressProvider(_dockerDiscoverySettings, _logger);
        }

        public override async Task<Resolved> Lookup(Lookup lookup, TimeSpan resolveTimeout)
        {
            var cancellationTokenSource = new CancellationTokenSource(resolveTimeout);

            var resolvedTargets = new List<ResolvedTarget>();
            if (_dockerDiscoverySettings?.Ports is null || !_dockerDiscoverySettings.Ports.Any())
            {
                return new Resolved(lookup.ServiceName, resolvedTargets);
            }

            var addresses = await _ipAddressProvider.GetIpAddressesAsync(cancellationTokenSource.Token);

            foreach (var address in addresses)
            {
                foreach (var port in _dockerDiscoverySettings.Ports)
                {
                    resolvedTargets.Add(new ResolvedTarget(host: address.ToString(), port: port, address: address));
                }
            }

            return new Resolved(lookup.ServiceName, resolvedTargets);
        }
    }
}