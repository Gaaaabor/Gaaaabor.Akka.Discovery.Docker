using Docker.DotNet;
using Docker.DotNet.Models;
using System.Text.RegularExpressions;

namespace Gaaaabor.Akka.Discovery.Docker.Tests
{
    public class DockerSwarmTests
    {
        [Fact]
        public async Task CreateDockerSwarmInDocker()
        {
            try
            {
                var cancellationToken = new CancellationTokenSource().Token;

                var dockerClientConfiguration = new DockerClientConfiguration();
                using var dockerClient = dockerClientConfiguration.CreateClient();

                var containerId1Task = PullAndRunContainerAsync(dockerClient, $"Test_{Guid.NewGuid():N}", cancellationToken);
                var containerId2Task = PullAndRunContainerAsync(dockerClient, $"Test_{Guid.NewGuid():N}", cancellationToken);
                var containerId3Task = PullAndRunContainerAsync(dockerClient, $"Test_{Guid.NewGuid():N}", cancellationToken);

                await Task.WhenAll(containerId1Task, containerId2Task, containerId3Task);

                await Task.Delay(5000);

                var joinCommand = await CreateSwarmAsync(dockerClient, containerId1Task.Result, cancellationToken);
                await JoinSwarmAsync(dockerClient, containerId2Task.Result, joinCommand, cancellationToken);
                await JoinSwarmAsync(dockerClient, containerId3Task.Result, joinCommand, cancellationToken);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        private async Task<string> PullAndRunContainerAsync(DockerClient dockerClient, string containerName, CancellationToken cancellationToken)
        {
            var image = "library/docker:dind";
            await dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
            {
                FromImage = image
            }, new AuthConfig(), new Progress<JSONMessage>(), cancellationToken);

            // Volume CA
            await dockerClient.Volumes.CreateAsync(new VolumesCreateParameters { Name = "did-certs-ca" });

            // Volume Certs
            await dockerClient.Volumes.CreateAsync(new VolumesCreateParameters { Name = "did-certs-client" });

            //var networkName = "weather-network";

            //var networkFilter = new Dictionary<string, IDictionary<string, bool>>
            //{
            //    { "name", new Dictionary<string, bool> { { networkName, true } } }
            //};

            //string networkId;
            //var networks = await dockerClient.Networks.ListNetworksAsync(new NetworksListParameters { Filters = networkFilter }, cancellationToken);
            //if (networks.Count > 0)
            //{
            //    networkId = networks.First().ID;
            //}
            //else
            //{
            //    var networkResult = await dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
            //    {
            //        Attachable = true,
            //        Name = networkName
            //    }, cancellationToken);

            //    networkId = networkResult.ID;
            //}

            var containerResult = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Name = containerName,
                Image = "docker:dind",
                Env = new List<string> { "DOCKER_TLS_CERTDIR=/certs" },
                HostConfig = new HostConfig
                {
                    Binds = new List<string>
                    {
                        "did-certs-ca:/certs/ca",
                        "did-certs-client:/certs/client"
                    },
                    Privileged = true
                },
                AttachStderr = true,
                AttachStdin = true,
                AttachStdout = true,
                Tty = true,

            }, cancellationToken);

            //await dockerClient.Networks.ConnectNetworkAsync(networkId, new NetworkConnectParameters
            //{
            //    Container = containerResult.ID
            //});

            await dockerClient.Containers.StartContainerAsync(containerResult.ID, new ContainerStartParameters(), cancellationToken);

            return containerResult.ID;
        }

        private async Task<string> CreateSwarmAsync(DockerClient dockerClient, string containerId, CancellationToken cancellationToken)
        {
            var execResponse = await dockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters()
            {
                AttachStderr = true,
                AttachStdout = true,
                Privileged = true,
                Tty = true,
                Cmd = ["docker", "swarm", "init"],
            });

            var multiplexedStream = await dockerClient.Exec.StartAndAttachContainerExecAsync(execResponse.ID, true);
            var response = await multiplexedStream.ReadOutputToEndAsync(cancellationToken);

            var match = Regex.Match(response.stdout, "docker swarm join --token(.*?)(\n)");
            if (match.Success)
            {
                var joinCommand = match.Value.Replace(Environment.NewLine, string.Empty);
                return joinCommand;
            }

            return string.Empty;
        }

        private async Task JoinSwarmAsync(DockerClient dockerClient, string containerId2, string joinCommand, CancellationToken cancellationToken)
        {
            var execResponse = await dockerClient.Exec.ExecCreateContainerAsync(containerId2, new ContainerExecCreateParameters()
            {
                AttachStderr = true,
                AttachStdout = true,
                Privileged = true,
                Tty = true,
                Cmd = joinCommand.Split(" "),
            });

            var multiplexedStream = await dockerClient.Exec.StartAndAttachContainerExecAsync(execResponse.ID, true);
            await multiplexedStream.ReadOutputToEndAsync(cancellationToken);
        }
    }
}
