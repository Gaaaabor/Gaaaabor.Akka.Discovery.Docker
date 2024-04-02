# Gaaaabor.Akka.Discovery.Docker

This package is created to make ServiceDiscovery with Docker easy while hosting an Akka cluster.

For a working example see the https://github.com/Gaaaabor/Gaaaabor.Akka.Discovery.Docker/tree/master/examples/DockerExample

# Sharding example with Docker Discovery

The docker-compose.yml contains the example project with 5 instance.

Before running set the "GeneratePackageOnBuild" to "False" in Gaaaabor.Akka.Discovery.Docker.csproj ("<GeneratePackageOnBuild>False</GeneratePackageOnBuild>" )

The cluster starts with 5 node by default, the example can be started using the following UP command:
docker compose -f PATH_TO_THE_REPO\Gaaaabor.Akka.Discovery.Docker\docker-compose.yml up --force-recreate --build

...and can be shut down with the following DOWN command:
docker compose -f PATH_TO_THE_REPO\Gaaaabor.Akka.Discovery.Docker\docker-compose.yml down

The example controller can be reached on the http://localhost:port/WeatherForecast endpoint.
The service discovery uses the Docker API with Dotnet.Docker to get the running containers and their IP addresses.

For more info on Docker.Dotnet check the project's site here https://www.nuget.org/profiles/Docker.DotNet
For more info in Akka Cluster check the project's site here https://getakka.net/articles/clustering/cluster-overview.html

The attached "ExampleContainerListResponse.json" contains a serialized example for a ContainerListResponse.
This response is the result of the Docker.DotNet library's "ListContainersAsync" method, this is used to search for containers as potential members of the cluster.
The example can be helpful to create ContainerFilters (API result filters).