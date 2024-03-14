using Akka.Actor;
using Akka.Hosting;
using DockerExample.Actors.Messages;
using DockerExample.Cluster;
using Microsoft.AspNetCore.Mvc;

namespace DockerExample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IActorRegistry _actorRegistry;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IActorRegistry actorRegistry)
        {
            _logger = logger;
            _actorRegistry = actorRegistry;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<List<WeatherForecastedEvent>> GetAsync()
        {
            _logger.LogInformation("[WeatherForecastController] - GetWeatherForecast - Invoked");

            var weatherForecastActor = _actorRegistry.Get<SimpleShardRegion>();
            var weatherForecastedEvent = await weatherForecastActor.Ask<List<WeatherForecastedEvent>>(new SimpleShardEnvelope
            {
                EntityId = Random.Shared.Next(1, 11).ToString(),
                Message = new ForecastWeatherCommand()
            });

            return weatherForecastedEvent;
        }
    }
}