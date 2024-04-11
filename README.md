# Gaaaabor.Akka.Discovery.Docker

This package is created to make ServiceDiscovery with Docker easy while hosting an Akka cluster.  
For a working example see the https://github.com/Gaaaabor/Gaaaabor.Akka.Discovery.Docker/tree/master/examples/DockerExample

# How to use the package

Upon setting up your actorsystem with .AddAkka(...) IServiceCollection extension method, you can use the .WithDockerDiscovery() AkkaConfigurationBuilder extension method to set up service discovery in 3 different ways:

### Without any filtering:
This way all the network interfaces and all container IP addresses will be used, this is useful when only the cluster's containers exists in the infrastructure with only 1 network interface.
```
builder.Services.AddAkka("weather", builder =>
{
    const int ManagementPort = 8558;
    const string Role = "WeatherForecast";

    builder
        .WithRemoting(hostname: Dns.GetHostName(), port: 8091)
        .WithClustering(new ClusterOptions { Roles = new[] { Role } })
        .WithClusterBootstrap(serviceName: "exampleservice")
        .WithAkkaManagement(port: ManagementPort)
        .WithDockerDiscovery();
});
```
### With DockerServiceDiscoveryOptions class:
```
builder.Services.AddAkka("weather", builder =>
{
    const int ManagementPort = 8558;
    const string Role = "WeatherForecast";

    builder
        .WithRemoting(hostname: Dns.GetHostName(), port: 8091)
        .WithClustering(new ClusterOptions { Roles = new[] { Role } })
        .WithClusterBootstrap(serviceName: "exampleservice")
        .WithAkkaManagement(port: ManagementPort)
        .WithDockerDiscovery(new DockerServiceDiscoveryOptions
        {
            Endpoint = "unix:///var/run/docker.sock",
            Ports = new() { ManagementPort },
            ContainersListParameters = new Docker.DotNet.Models.ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "status", new Dictionary<string, bool>
                        {
                            { "running", true}, // Filtering on running containers (API level filtering).
                            { "created", true}, // Filtering on created containers (API level filtering).
                        }
                    }
                }
            },
            ContainerFilters = new()
            {
                new Filter("names", "weather-example"), // Filtering on container names containing "weather-example".
                new Filter("labels", "com.docker.compose.service:weather-example") // Filtering on container labels where the label is "com.docker.compose.service" and its value is "weather-example".
            },
            NetworkNameFilter = "weather-bridge" // Filtering on a specific network from where we get the IP addresses of each filtered containers.
        });
});
```
### With an action:
```
builder.Services.AddAkka("weather", builder =>
{
    const int ManagementPort = 8558;
    const string Role = "WeatherForecast";

    builder
        .WithRemoting(hostname: Dns.GetHostName(), port: 8091)
        .WithClustering(new ClusterOptions { Roles = new[] { Role } })
        .WithClusterBootstrap(serviceName: "exampleservice")
        .WithAkkaManagement(port: ManagementPort)
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
                            { "running", true}, // Filtering on running containers (API level filtering).
                            { "created", true}, // Filtering on created containers (API level filtering).
                        }
                    }
                }
            };
            options.ContainerFilters = new()
            {
                new Filter("names", "weather-example"), // Filtering on container names containing "weather-example".
                new Filter("labels", "com.docker.compose.service:weather-example") // Filtering on container labels where the label is "com.docker.compose.service" and its value is "weather-example".
            };
            options.NetworkNameFilter = "weather-bridge"; // Filtering on a specific network from where we get the IP addresses of each filtered containers.
        });
});
```
It's a good practice to narrow down the containers and networks participating in the creation of the cluster as much as possible, using filters to avoid unnecessary hosts to participate in the cluster.

# Sharding example with Docker Discovery

Let's explore the contents of the Examples/DockerExample folder.  
The [docker-compose.yml](https://github.com/Gaaaabor/Gaaaabor.Akka.Discovery.Docker/blob/master/docker-compose.yml) contains the example project with 5 instances.  

Before running the project edit and set the "GeneratePackageOnBuild" to "False" in Gaaaabor.Akka.Discovery.Docker.csproj, to avoid errors during build. (This is only required for building the nuget package itself)
```
<GeneratePackageOnBuild>False</GeneratePackageOnBuild>
```

The cluster starts with 5 node by default, the example can be started using the following UP command:
```
docker compose -f PATH_TO_THE_REPO\Gaaaabor.Akka.Discovery.Docker\docker-compose.yml up --force-recreate --build
```

...and can be shut down with the following DOWN command:
```
docker compose -f PATH_TO_THE_REPO\Gaaaabor.Akka.Discovery.Docker\docker-compose.yml down
```

The example controller can be reached on the `http://localhost:PORT/WeatherForecast/GetWeatherForecast` endpoint.  
The service discovery uses the Docker API with Dotnet.Docker to get the running containers and their IP addresses.  

For more info on Docker.Dotnet check the project's site [Here](https://www.nuget.org/profiles/Docker.DotNet)  
For more info in Akka Cluster check the project's site [Here](https://getakka.net/articles/clustering/cluster-overview.html)  

The attached [ExampleContainerListResponse.json](https://github.com/Gaaaabor/Gaaaabor.Akka.Discovery.Docker/blob/master/examples/DockerExample/ExampleContainerListResponse.json) contains a serialized example for a ContainerListResponse.  
This is the response of the Docker.DotNet library's "ListContainersAsync" method used to search for containers as potential members of the cluster.  
I attached the example to help create ContainerFilters (API result filters).  
