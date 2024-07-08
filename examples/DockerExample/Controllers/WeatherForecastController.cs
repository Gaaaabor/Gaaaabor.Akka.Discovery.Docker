using Akka.Actor;
using Akka.Hosting;
using DockerExample.Actors.Messages;
using DockerExample.Cluster;
using Microsoft.AspNetCore.Mvc;

namespace DockerExample.Controllers
{
    [ApiController]
    [Route("/")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IActorRegistry _actorRegistry;
        private readonly TimeSpan _askTimeoutInSec = TimeSpan.FromSeconds(2);

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IActorRegistry actorRegistry)
        {
            _logger = logger;
            _actorRegistry = actorRegistry;
        }

        public async Task<List<WeatherForecastedEvent>> GetAsync(CancellationToken cancellationToken)
        {
            var entityId = Random.Shared.Next(1, 100).ToString();

            _logger.LogInformation($"[WeatherForecastController] - Sent ForecastWeatherCommand for Entity \"{entityId}\"");

            var weatherForecastActor = _actorRegistry.Get<SimpleShardRegion>();
            var weatherForecastedEvent = await weatherForecastActor.Ask<List<WeatherForecastedEvent>>(new SimpleShardEnvelope
            {
                EntityId = entityId,
                Message = new ForecastWeatherCommand()
            }, _askTimeoutInSec, cancellationToken);

            return weatherForecastedEvent;
        }
    }
}