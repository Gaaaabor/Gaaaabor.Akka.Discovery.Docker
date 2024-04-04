using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Discovery.Config.Hosting;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Akka.Remote.Hosting;
using Docker.DotNet;
using Docker.DotNet.Models;
using DockerExample.Cluster;
using Gaaaabor.Akka.Discovery.Docker;
using Newtonsoft.Json;
using System.Net;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAkka("weather", builder =>
{
    const int ManagementPort = 8558;
    const string Role = "WeatherForecast";

    var useSwarm = bool.TryParse(Environment.GetEnvironmentVariable("UseSwarm"), out var rawUseSwarm) && rawUseSwarm;

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
        //    ContainersListParameters = new Docker.DotNet.Models.ContainersListParameters
        //    {
        //        All = true,
        //        Filters = new Dictionary<string, IDictionary<string, bool>>
        //        {
        //            { "status", new Dictionary<string, bool>
        //                {
        //                    { "running", true},
        //                    { "created", true},
        //                }
        //            }
        //        }
        //    },
        //    ContainerFilters = new() { new Filter("names", "weather-example"), new Filter("labels", "com.docker.compose.service:weather-example") },
        //    NetworkNameFilter = "weather-bridge",
        //})
        // OR
        .WithDockerDiscovery(options =>
        {
            options.Endpoint = "unix:///var/run/docker.sock";
            options.Ports = new() { ManagementPort };
            options.ContainersListParameters = new Docker.DotNet.Models.ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "status", new Dictionary<string, bool>
                        {
                            { "running", true}
                        }
                    }
                }
            };
            options.ContainerFilters = new()
            {
                new Filter("names", "weather-example"),
                new Filter("labels", "com.docker.compose.service:weather-example")
            };
            options.UseSwarm = useSwarm;
            //options.NetworkNameFilter = "weather-bridge";
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
app.MapGet("/services", async httpContext =>
{
    var services = new List<SwarmService>();
    var dockerClientConfiguration = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"));
    using (var client = dockerClientConfiguration.CreateClient())
    {
        var swarmServices = await client.Swarm.ListServicesAsync(new ServicesListParameters(), CancellationToken.None);
        if (swarmServices != null)
        {
            foreach (var swarmService in swarmServices)
            {
                SwarmService service = await client.Swarm.InspectServiceAsync(swarmService.ID);
                services.Add(service);
            }
        }
    }

    await httpContext.Response.WriteAsJsonAsync(services, CancellationToken.None);
});
app.Run();