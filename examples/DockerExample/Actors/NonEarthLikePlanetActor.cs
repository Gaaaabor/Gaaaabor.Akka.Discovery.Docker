using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using DockerExample.Actors.Messages;
using System.Net;

namespace DockerExample.Actors
{
    public class NonEarthLikePlanetActor : ReceivePersistentActor
    {
        private static readonly string[] Summaries = ["FreezingHell", "ScrochingHell", "FaceIsFrozenBackIsScorched", "FaceIsScorchedBackIsFrozen"];

        private readonly string _hostName;

        public override string PersistenceId { get; }

        public NonEarthLikePlanetActor(string entityId)
        {
            PersistenceId = entityId;
            _hostName = Dns.GetHostName();

            Command<ForecastWeatherCommand>(OnForecastWeatherCommand);
        }

        protected override void PreStart()
        {
            Log.Info("Actor {0} - {1} started", PersistenceId, nameof(NonEarthLikePlanetActor));
        }

        protected override void PostStop()
        {
            Log.Info("Actor {0} - {1} stopped", PersistenceId, nameof(NonEarthLikePlanetActor));
        }

        private void OnForecastWeatherCommand(ForecastWeatherCommand forecastWeatherCommand)
        {
            var weatherForecastedEvents = new List<WeatherForecastedEvent>();

            Log.Info("Actor {0} - {1} received a message", PersistenceId, nameof(NonEarthLikePlanetActor));

            for (int i = 0; i < Random.Shared.Next(1, 15); i++)
            {
                var factor = Random.Shared.Next(0, Summaries.Length);
                var date = DateTime.UtcNow.AddDays(i);
                weatherForecastedEvents.Add(new WeatherForecastedEvent
                {
                    Date = date,
                    TemperatureC = factor * 5,
                    Summary = $"The weather in NonEarthLikePlanet {PersistenceId} on host {_hostName} {(i == 0 ? "is" : "will be")} {Summaries[factor]}"
                });
            }

            Sender.Tell(weatherForecastedEvents);
        }
    }
}
