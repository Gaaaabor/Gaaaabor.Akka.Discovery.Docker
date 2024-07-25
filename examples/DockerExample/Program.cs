using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Akka.Persistence.Redis.Hosting;
using Akka.Remote.Hosting;
using DockerExample.Actors;
using DockerExample.Cluster;
using Gaaaabor.Akka.Discovery.Docker;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAkka("weather", builder =>
{
    const int ManagementPort = 8558;

    var useSwarm = bool.TryParse(Environment.GetEnvironmentVariable("UseSwarm"), out var rawUseSwarm) && rawUseSwarm;
    var connectionString = Environment.GetEnvironmentVariable("ConnectionString");

    builder
        .WithRemoting(hostname: Dns.GetHostName(), port: 8091)
        .WithClustering(new ClusterOptions { Roles = new[] { "EarthLike", "NonEarthLike" } })
        .WithClusterBootstrap()
        .WithAkkaManagement(port: ManagementPort)
        .WithDockerDiscovery(options =>
        {
            options.Endpoint = "unix:///var/run/docker.sock";
            options.Ports = new() { ManagementPort };
            options.ContainersListParameters = new Docker.DotNet.Models.ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>> { { "status", new Dictionary<string, bool> { { "running", true } } } }
            };
            options.ContainerFilters = new()
            {
                new Filter("names", "weather-example"),
                new Filter("labels", "com.docker.compose.service:weather-example")
            };
            options.UseSwarm = useSwarm;
            options.NetworkNameFilter = "weather-bridge";
        })
        .WithRedisPersistence(
            journal => { journal.ConfigurationString = connectionString; },
            snapshot => { snapshot.ConfigurationString = connectionString; })
        
        // This way the ShardRegions act similar, despite the different configuration
        //.WithShardRegion<EarthLikePlanetShardRegion>( // EarthLikePlanetShardRegion is just a "marker" class
        //    nameof(EarthLikePlanetShardRegion),
        //    entityId => Props.Create<EarthLikePlanetActor>(entityId),
        //    new SimpleMessageExtractor(), new ShardOptions
        //    {
        //        Role = "EarthLike",
        //        RememberEntities = true,
        //        ShouldPassivateIdleEntities = false,
        //        StateStoreMode = Akka.Cluster.Sharding.StateStoreMode.Persistence,
        //        JournalOptions = new RedisJournalOptions(),
        //        SnapshotOptions = new RedisSnapshotOptions()
        //    })
        //.WithShardRegion<NonEarthLikePlanetShardRegion>( // NonEarthLikePlanetShardRegion is just a "marker" class
        //    nameof(NonEarthLikePlanetShardRegion),
        //    entityId => Props.Create<NonEarthLikePlanetActor>(entityId),
        //    new SimpleMessageExtractor(), new ShardOptions
        //    {
        //        Role = "NonEarthLike",
        //        RememberEntities = false,
        //        ShouldPassivateIdleEntities = true,
        //        PassivateIdleEntityAfter = TimeSpan.FromSeconds(5),
        //        StateStoreMode = Akka.Cluster.Sharding.StateStoreMode.Persistence,
        //        JournalOptions = new RedisJournalOptions(),
        //        SnapshotOptions = new RedisSnapshotOptions()
        //    })
        .StartActors((system, reg) =>
        {
            var earthLikeSettings = ClusterShardingSettings
                .Create(system)
                .WithRole("EarthLike")
                .WithRememberEntities(true)
                .WithStateStoreMode(Akka.Cluster.Sharding.StateStoreMode.Persistence);

            var earthLikePlanetShardRegion = ClusterSharding.Get(system).Start(
                typeName: nameof(EarthLikePlanetShardRegion),
                entityPropsFactory: entityId => Props.Create<EarthLikePlanetActor>(entityId),
                settings: earthLikeSettings,
                messageExtractor: new SimpleMessageExtractor());

            reg.Register<EarthLikePlanetShardRegion>(earthLikePlanetShardRegion);

            var nonEarthLikeSettings = ClusterShardingSettings
                .Create(system)
                .WithRole("NonEarthLike")
                .WithRememberEntities(false)
                .WithPassivateIdleAfter(TimeSpan.FromSeconds(5))
                .WithStateStoreMode(Akka.Cluster.Sharding.StateStoreMode.Persistence);

            var nonEarthLikePlanetShardRegion = ClusterSharding.Get(system).Start(
                typeName: nameof(NonEarthLikePlanetShardRegion),
                entityPropsFactory: entityId => Props.Create<NonEarthLikePlanetActor>(entityId),
                settings: nonEarthLikeSettings,
                messageExtractor: new SimpleMessageExtractor());

            reg.Register<NonEarthLikePlanetShardRegion>(nonEarthLikePlanetShardRegion);
        });
});

var app = builder.Build();
app.MapControllers();
app.Run();