using Akka.Actor.Setup;
using Akka.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gaaaabor.Akka.Discovery.Docker
{
    public class DockerServiceDiscoveryOptions : IHoconOption
    {
        private const string FullPath = "akka.discovery.docker";

        public string ConfigPath { get; } = "docker";

        public Type Class { get; } = typeof(DockerServiceDiscovery);

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
        public string Endpoint { get; set; } = "unix:///var/run/docker.sock";

        /// <summary>
        /// Builds the HOCON config
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="setup"></param>
        public void Apply(AkkaConfigurationBuilder builder, Setup setup = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{FullPath} {{");
            sb.AppendLine($"class = {Class.AssemblyQualifiedName.ToHocon()}");

            if (ContainerFilters != null)
            {
                var filters = ContainerFilters
                    .SelectMany(filter => filter.Values.Select(value => (filter.Name, Tag: value)))
                    .Select(t => $"{t.Name}={t.Tag}");

                sb.AppendLine($"containerfilters = {string.Join(";", filters).ToHocon()}");
            }

            if (!string.IsNullOrEmpty(NetworkNameFilter))
            {
                sb.AppendLine($"networknamefilter = {NetworkNameFilter.ToHocon()}");
            }

            if (Ports != null)
            {
                sb.AppendLine($"ports = [{string.Join(",", Ports)}]");
            }

            if (!string.IsNullOrEmpty(Endpoint))
            {
                sb.AppendLine($"endpoint = {Endpoint.ToHocon()}");
            }

            sb.AppendLine("}");

            builder.AddHocon(sb.ToString(), HoconAddMode.Prepend);
        }
    }
}
