using Akka.Actor;
using DockerExample.Actors.Messages;
using System.Net;

namespace DockerExample.Actors
{
    public class WeatherForecastActor : ReceiveActor
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly string _entityId;

        public WeatherForecastActor(string entityId)
        {
            _entityId = entityId;
            Receive<ForecastWeatherCommand>(OnForecastWeatherCommand);
        }

        private void OnForecastWeatherCommand(ForecastWeatherCommand forecastWeatherCommand)
        {
            var weatherForecastedEvents = new List<WeatherForecastedEvent>();
            var host = Dns.GetHostName();

            for (int i = 0; i < Random.Shared.Next(1, 15); i++)
            {
                var factor = Random.Shared.Next(0, Summaries.Length);

                var date = DateTime.UtcNow.AddDays(i);
                weatherForecastedEvents.Add(new WeatherForecastedEvent
                {
                    Date = date,
                    TemperatureC = factor * 5,
                    Summary = $"The weather in actor {_entityId} on host {host} {(i == 0 ? "is" : "will be")} {Summaries[factor]}"
                });
            }

            Sender.Tell(weatherForecastedEvents);
        }
    }
}
