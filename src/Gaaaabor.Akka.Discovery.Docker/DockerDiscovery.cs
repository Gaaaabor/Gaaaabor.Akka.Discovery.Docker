using Akka.Actor;
using Akka.Configuration;

namespace Gaaaabor.Akka.Discovery.Docker
{
    public class DockerDiscovery : IExtension
    {
        public readonly DockerDiscoverySettings Settings;

        public static Config DefaultConfiguration()
        {
            return ConfigurationFactory.FromObject(new DockerServiceDiscoveryOptions());
        }

        public static DockerDiscovery Get(ActorSystem system)
        {
            return system.WithExtension<DockerDiscovery, DockerDiscoveryProvider>();
        }

        public DockerDiscovery(ExtendedActorSystem system)
        {
            system.Settings.InjectTopLevelFallback(DefaultConfiguration());
            Settings = DockerDiscoverySettings.Create(system);

            var setup = system.Settings.Setup.Get<DockerDiscoverySetup>();
            if (setup.HasValue)
            {
                Settings = setup.Value.Apply(Settings);
            }
        }
    }

    public class DockerDiscoveryProvider : ExtensionIdProvider<DockerDiscovery>
    {
        public override DockerDiscovery CreateExtension(ExtendedActorSystem system) => new DockerDiscovery(system);
    }
}
