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

        public WeatherForecastController(
            ILogger<WeatherForecastController> logger,
            IActorRegistry actorRegistry)
        {
            _logger = logger;
            _actorRegistry = actorRegistry;
        }

        public async Task<ActionResult> GetAsync(CancellationToken cancellationToken)
        {
            var entityId = Random.Shared.Next(1, 100).ToString();

            _logger.LogInformation($"[WeatherForecastController] - Sent ForecastWeatherCommand for Entity \"{entityId}\"");

            var message = new SimpleShardEnvelope
            {
                EntityId = entityId,
                Message = new ForecastWeatherCommand()
            };

            var nonEarthLikePlanetShardRegion = _actorRegistry.Get<NonEarthLikePlanetShardRegion>();
            var nonEarthLikeResult = await nonEarthLikePlanetShardRegion
                .Ask<List<WeatherForecastedEvent>>(message, _askTimeoutInSec, cancellationToken);

            var earthLikePlanetShardRegion = _actorRegistry.Get<EarthLikePlanetShardRegion>();
            var earthLikeResult = await earthLikePlanetShardRegion
                .Ask<List<WeatherForecastedEvent>>(message, _askTimeoutInSec, cancellationToken);

            return Ok(new[] { nonEarthLikeResult, earthLikeResult });
        }
    }
}