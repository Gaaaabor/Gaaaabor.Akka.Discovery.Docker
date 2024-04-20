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

namespace Gaaaabor.Akka.Discovery.Docker.Providers
{
    public abstract class IpAddressProviderBase
    {
        protected DockerDiscoverySettings DockerDiscoverySettings { get; }
        protected ILoggingAdapter Logger { get; }
        protected DockerClientConfiguration DockerClientConfiguration { get; }
        protected Dictionary<string, Func<Filter, ContainerListResponse, bool>> ExpressionCache { get; }

        public IpAddressProviderBase(DockerDiscoverySettings dockerDiscoverySettings, ILoggingAdapter logger)
        {
            DockerDiscoverySettings = dockerDiscoverySettings;
            Logger = logger;

            var endpoint = DockerDiscoverySettings.Endpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new Exception("Endpoint cannot be null or empty!");
            }

            ExpressionCache = new Dictionary<string, Func<Filter, ContainerListResponse, bool>>(StringComparer.OrdinalIgnoreCase);
            DockerClientConfiguration = new DockerClientConfiguration(new Uri(endpoint));

            BuildSimpleExpressionCache();
        }

        public abstract Task<List<IPAddress>> GetIpAddressesAsync(CancellationToken cancellationToken);

        private void BuildSimpleExpressionCache()
        {
            var properties = typeof(ContainerListResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                switch (property.Name)
                {
                    case string name when nameof(ContainerListResponse.ID).Equals(name, StringComparison.OrdinalIgnoreCase):
                        ExpressionCache.Add(property.Name, (filter, container) => filter.Values.Any(filterValue => container.ID.Contains(filterValue)));
                        break;

                    case string name when nameof(ContainerListResponse.Image).Equals(name, StringComparison.OrdinalIgnoreCase):
                        ExpressionCache.Add(property.Name, (filter, container) => filter.Values.Any(filterValue => container.Image.Contains(filterValue)));
                        break;

                    case string name when nameof(ContainerListResponse.ImageID).Equals(name, StringComparison.OrdinalIgnoreCase):
                        ExpressionCache.Add(property.Name, (filter, container) => filter.Values.Any(filterValue => container.ImageID.Contains(filterValue)));
                        break;

                    case string name when nameof(ContainerListResponse.State).Equals(name, StringComparison.OrdinalIgnoreCase):
                        ExpressionCache.Add(property.Name, (filter, container) => filter.Values.Any(filterValue => container.State.Contains(filterValue)));
                        break;

                    case string name when nameof(ContainerListResponse.Names).Equals(name, StringComparison.OrdinalIgnoreCase):
                        ExpressionCache.Add(property.Name, (filter, container) => filter.Values.Any(filterValue => container.Names.Any(containerName => containerName.Contains(filterValue))));
                        break;

                    case string name when nameof(ContainerListResponse.Labels).Equals(name, StringComparison.OrdinalIgnoreCase):

                        ExpressionCache.Add(property.Name, (filter, container) =>
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

            Logger.Info("[DockerServiceDiscovery] Simple expression cache successfully built.");
        }
    }
}