using Akka.Actor;
using DockerExample.Actors;

namespace DockerExample.Cluster
{
    public class SimpleShardRegion
    {
        public static Props ActorFactory(string entityId)
        {
            return Props.Create<WeatherForecastActor>(entityId);
        }
    }
}
