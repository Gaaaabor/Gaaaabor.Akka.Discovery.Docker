using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Akka.Remote.Hosting;
using DockerExample.Cluster;
using Gaaaabor.Akka.Discovery.Docker;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAkka("weather", builder =>
{
    const int ManagementPort = 8558;
    const string Role = "WeatherForecast";

    builder
        .WithRemoting(hostname: Dns.GetHostName(), port: 8091)
        .WithClustering(new ClusterOptions { Roles = new[] { Role } })
        .WithClusterBootstrap(serviceName: "exampleservice")
        .WithAkkaManagement(port: ManagementPort)
        //.WithDockerDiscovery()
        // OR
        //.WithDockerDiscovery(new DockerServiceDiscoveryOptions
        //{
        //    Endpoint = "unix:///var/run/docker.sock",
        //    Ports = new() { ManagementPort },
        //    ContainerFilters = new() { new Filter("status", "running") },
        //    NetworkNameFilter = "weather-bridge",
        //})
        // OR
        .WithDockerDiscovery(options =>
        {
            options.Endpoint = "unix:///var/run/docker.sock";
            options.Ports = new() { ManagementPort };
            options.ContainerFilters = new()
            {
                new Filter("status", "running")
            };
            options.NetworkNameFilter = "weather-bridge";
        })
        .WithShardRegion<SimpleShardRegion>(nameof(SimpleShardRegion), SimpleShardRegion.ActorFactory, new SimpleMessageExtractor(), new ShardOptions
        {
            Role = Role
        })
        .WithActors((actorSystem, actorRegistry) =>
        {
            var simpleShardRegion = actorRegistry.Get<SimpleShardRegion>();
            simpleShardRegion.Tell(new ShardRegion.StartEntity("1"));
        });
});

var app = builder.Build();
app.UseAuthorization();
app.MapControllers();
app.Run();
