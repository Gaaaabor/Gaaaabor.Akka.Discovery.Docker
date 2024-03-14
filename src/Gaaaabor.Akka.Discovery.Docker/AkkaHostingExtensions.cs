using Akka.Hosting;
using Microsoft.Extensions.Options;
using System;

namespace Gaaaabor.Akka.Discovery.Docker
{
    public static class AkkaHostingExtensions
    {
        /// <summary>
        ///     Adds Gaaaabor.Akka.Discovery.Docker support to the <see cref="ActorSystem"/>.
        ///     Note that this only adds the discovery plugin, you will still need to add ClusterBootstrap for
        ///     a complete solution.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <example>
        ///   <code>
        ///     services.AddAkka("mySystem", builder => {
        ///         builder
        ///             .WithClustering()
        ///             .WithClusterBootstrap(options =>
        ///             {
        ///                 options.ContactPointDiscovery.ServiceName = "testService";
        ///                 options.ContactPointDiscovery.RequiredContactPointsNr = 1;
        ///             }, autoStart: true)
        ///             .WithDockerDiscovery();
        ///     });
        ///   </code>
        /// </example>
        public static AkkaConfigurationBuilder WithDockerDiscovery(this AkkaConfigurationBuilder builder) => builder.WithDockerDiscovery(new DockerServiceDiscoveryOptions());

        /// <summary>
        ///     Adds Gaaaabor.Akka.Discovery.Docker support to the <see cref="ActorSystem"/>.
        ///     Note that this only adds the discovery plugin, you will still need to add ClusterBootstrap for
        ///     a complete solution.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="configure">
        ///     An action that modifies an <see cref="DockerDiscoverySetup"/> instance, used
        ///     to configure Gaaaabor.Akka.Discovery.Docker.
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <example>
        ///   <code>
        ///     services.AddAkka("mySystem", builder => {
        ///         builder
        ///             .WithClustering()
        ///             .WithClusterBootstrap(options =>
        ///             {
        ///                 options.ContactPointDiscovery.ServiceName = "testService";
        ///                 options.ContactPointDiscovery.RequiredContactPointsNr = 1;
        ///             }, autoStart: true)
        ///             .WithDockerDiscovery(options =>
        ///             {
        ///                 options.Endpoint = "unix:///var/run/docker.sock";
        ///                 options.Ports = new () { ManagementPort };
        ///                 options.ContainerFilters = new ()
        ///                 {
        ///                     new Filter("status", "running")
        ///                 };
        ///                 options.NetworkNameFilter = "weather-bridge";
        ///             });
        ///     });
        ///   </code>
        /// </example>
        public static AkkaConfigurationBuilder WithDockerDiscovery(this AkkaConfigurationBuilder builder, Action<DockerServiceDiscoveryOptions> configure)
        {
            var options = new DockerServiceDiscoveryOptions();
            configure(options);
            return builder.WithDockerDiscovery(options);
        }

        /// <summary>
        ///     Adds Gaaaabor.Akka.Discovery.Docker support to the <see cref="ActorSystem"/>.
        ///     Note that this only adds the discovery plugin, you will still need to add ClusterBootstrap for
        ///     a complete solution.
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="options">
        ///     The <see cref="DockerDiscoverySetup"/> instance used to configure Gaaaabor.Akka.Discovery.Docker.
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        /// <example>
        ///   <code>
        ///     services.AddAkka("mySystem", builder => {
        ///         builder
        ///             .WithClustering()
        ///             .WithClusterBootstrap(options =>
        ///             {
        ///                 options.ContactPointDiscovery.ServiceName = "testService";
        ///                 options.ContactPointDiscovery.RequiredContactPointsNr = 1;
        ///             }, autoStart: true)
        ///             .WithDockerDiscovery(new DockerServiceDiscoveryOptions
        ///             {
        ///                 Endpoint = "unix:///var/run/docker.sock",
        ///                 Ports = new () { ManagementPort },
        ///                 ContainerFilters = new () { new Filter("status", "running") },
        ///                 NetworkNameFilter = "weather-bridge",
        ///             })
        ///     });
        ///   </code>
        /// </example>
        public static AkkaConfigurationBuilder WithDockerDiscovery(this AkkaConfigurationBuilder builder, DockerServiceDiscoveryOptions options)
        {
            builder.AddHocon($"akka.discovery.method = {options.ConfigPath}", HoconAddMode.Prepend);
            options.Apply(builder);
            builder.AddHocon(DockerDiscovery.DefaultConfiguration(), HoconAddMode.Append);

            // force start the module
            builder.AddStartup((system, registry) =>
            {
                DockerDiscovery.Get(system);
            });
            return builder;
        }
    }
}
