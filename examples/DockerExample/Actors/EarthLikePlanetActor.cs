using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using DockerExample.Actors.Messages;
using System.Net;

namespace DockerExample.Actors
{
    public class EarthLikePlanetActor : ReceivePersistentActor
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching", "Rainy"
        };
        
        private readonly string _hostName;

        public override string PersistenceId { get; }

        public EarthLikePlanetActor(string entityId)
        {
            PersistenceId = entityId;
            _hostName = Dns.GetHostName();

            Command<ForecastWeatherCommand>(OnForecastWeatherCommand);
        }

        protected override void PreStart()
        {
            Log.Info("Actor {0} - {1} started", PersistenceId, nameof(EarthLikePlanetActor));
        }

        protected override void PostStop()
        {
            Log.Info("Actor {0} - {1} stopped", PersistenceId, nameof(EarthLikePlanetActor));
        }

        private void OnForecastWeatherCommand(ForecastWeatherCommand forecastWeatherCommand)
        {
            var weatherForecastedEvents = new List<WeatherForecastedEvent>();

            Log.Info("Actor {0} - {1} received a message", PersistenceId, nameof(EarthLikePlanetActor));

            for (int i = 0; i < Random.Shared.Next(1, 15); i++)
            {
                var factor = Random.Shared.Next(0, Summaries.Length);
                var date = DateTime.UtcNow.AddDays(i);
                weatherForecastedEvents.Add(new WeatherForecastedEvent
                {
                    Date = date,
                    TemperatureC = factor * 5,
                    Summary = $"The weather on a EarthLikePlanet {PersistenceId} on host {_hostName} {(i == 0 ? "is" : "will be")} {Summaries[factor]}"
                });
            }

            Sender.Tell(weatherForecastedEvents);
        }
    }
}
