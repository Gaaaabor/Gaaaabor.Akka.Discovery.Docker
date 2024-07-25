using Docker.DotNet.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace Gaaaabor.Akka.Discovery.Docker.Tests
{
    public class HostDockerSwarmInDockerTest : IClassFixture<DockerClientFixture>
    {
        private readonly DockerClientFixture _dockerClientFixture;
        private readonly ITestOutputHelper _testOutputHelper;

        public HostDockerSwarmInDockerTest(DockerClientFixture dockerClientFixture, ITestOutputHelper testOutputHelper)
        {
            _dockerClientFixture = dockerClientFixture;
            _testOutputHelper = testOutputHelper;
        }

        //[Fact] //Ignore this is for experiments
        public async Task CreateDockerSwarmInDocker()
        {
            try
            {
                var numberOfSwarmNodes = 6;
                var networkName = "weather-bridge";
                var email = Environment.GetEnvironmentVariable("TestEmail");
                var password = Environment.GetEnvironmentVariable("TestPass");
                var numberOfReplicas = 8;
                var createManagerJoinToken = true; // The only supported mode is when all the nodes are managers....

                var cancellationToken = new CancellationTokenSource().Token;
                var networkId = await CreateOrGetNetworkAsync(networkName, cancellationToken);

                var pullAndRunTasks = new List<Task<string>>();

                _testOutputHelper.WriteLine($"Pulling and starting {numberOfSwarmNodes} containers");

                for (int i = 0; i < numberOfSwarmNodes; i++)
                {
                    pullAndRunTasks.Add(PullAndRunContainerAsync($"DockerInDocker_{Guid.NewGuid():N}", networkId, "0", cancellationToken));
                }

                var containerIds = await Task.WhenAll(pullAndRunTasks);
                var firstContainerId = containerIds[0];

                // Let's give some time for containers to init...
                await Task.Delay(5000);

                _testOutputHelper.WriteLine("Initializing swarm");

                // The first container creates the swarm and returns the manager join command with the token
                var joinCommand = await CreateSwarmAsync(firstContainerId, networkId, createManagerJoinToken, cancellationToken);

                // Join rest of the nodes to the swarm
                var joinTasks = new List<Task>();
                foreach (var containerId in containerIds[1..])
                {
                    joinTasks.Add(JoinSwarmAsync(containerId, joinCommand, cancellationToken));
                    _testOutputHelper.WriteLine($"Adding container \"{containerId}\" to the swarm as {(createManagerJoinToken ? "manager" : "worker")} ");
                }

                await Task.WhenAll(joinTasks);

                var authAndPullTasks = new List<Task>();

                foreach (var containerId in containerIds)
                {
                    var authAndPullTask = AuthenticateNodeInSwarmAsync(containerId, email, password, cancellationToken)
                        .ContinueWith(x =>
                        {
                            return x.IsCompletedSuccessfully
                                ? PullImageForNodeInSwarmAsync(containerId, cancellationToken)
                                : Task.CompletedTask;
                        });

                    authAndPullTasks.Add(authAndPullTask);
                }

                await Task.WhenAll(authAndPullTasks);

                _testOutputHelper.WriteLine($"Creating swarm network \"{networkName}\"");

                await CreateSwarmNetworkAsync(firstContainerId, networkName, cancellationToken);

                _testOutputHelper.WriteLine($"Creating swarm service with \"{numberOfReplicas}\" replicas");

                await CreateSwarmServiceAsync(firstContainerId, networkName, numberOfReplicas, cancellationToken);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        private async Task AuthenticateNodeInSwarmAsync(string containerId, string email, string password, CancellationToken cancellationToken)
        {
            var execResponse = await _dockerClientFixture.DockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters()
            {
                AttachStderr = true,
                AttachStdout = true,
                Privileged = true,
                Tty = true,
                Cmd = ["docker", "login", "ghcr.io", "-u", email, "-p", password],
            });

            var multiplexedStream = await _dockerClientFixture.DockerClient.Exec.StartAndAttachContainerExecAsync(execResponse.ID, true);
            var response = await multiplexedStream.ReadOutputToEndAsync(cancellationToken);
            Debug.WriteLine(response);
        }

        private async Task PullImageForNodeInSwarmAsync(string containerId, CancellationToken cancellationToken)
        {
            var execResponse = await _dockerClientFixture.DockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters()
            {
                AttachStderr = true,
                AttachStdout = true,
                Privileged = true,
                Tty = true,
                Cmd = ["docker", "pull", "ghcr.io/gaaaabor/gaaaabor.akka.discovery.docker/gaaaabor.akka.discovery.docker.dockerexample:docker_swarm_support"],
            });

            var multiplexedStream = await _dockerClientFixture.DockerClient.Exec.StartAndAttachContainerExecAsync(execResponse.ID, true);
            var response = await multiplexedStream.ReadOutputToEndAsync(cancellationToken);
        }

        private async Task CreateSwarmNetworkAsync(string containerId, string networkName, CancellationToken cancellationToken)
        {
            var command = new[]
            {
                "docker","network","create","--scope=swarm","--attachable","-d","overlay",networkName
            };

            var execResponse = await _dockerClientFixture.DockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters()
            {
                AttachStderr = true,
                AttachStdout = true,
                Privileged = true,
                Tty = true,
                Cmd = command,
            });

            var multiplexedStream = await _dockerClientFixture.DockerClient.Exec.StartAndAttachContainerExecAsync(execResponse.ID, true);
            var response = await multiplexedStream.ReadOutputToEndAsync(cancellationToken);
        }

        private async Task CreateSwarmServiceAsync(string containerId, string networkName, int numberOfReplicas, CancellationToken cancellationToken)
        {
            var command = new[]
            {
                "docker", "service", "create",
                "--name", "testservice",
                "--replicas-max-per-node","1",
                "--replicas", numberOfReplicas.ToString(),
                "--mount", "type=bind,src=/var/run/docker.sock,dst=/var/run/docker.sock",
                "--container-label", "com.docker.compose.service=weather-example",
                "--env", "ASPNETCORE_ENVIRONMENT=Development",
                "--env", "ASPNETCORE_HTTP_PORTS=80",
                "--env", "UseSwarm=True",
                "--network", networkName,
                "--publish", "80:80",
                "ghcr.io/gaaaabor/gaaaabor.akka.discovery.docker/gaaaabor.akka.discovery.docker.dockerexample:docker_swarm_support"
            };

            var execResponse = await _dockerClientFixture.DockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters()
            {
                AttachStderr = true,
                AttachStdout = true,
                Privileged = true,
                Tty = true,
                Cmd = command,
            });

            var multiplexedStream = await _dockerClientFixture.DockerClient.Exec.StartAndAttachContainerExecAsync(execResponse.ID, true);
            var response = await multiplexedStream.ReadOutputToEndAsync(cancellationToken);
        }

        private async Task<string> CreateOrGetNetworkAsync(string networkName, CancellationToken cancellationToken)
        {
            var networkFilter = new Dictionary<string, IDictionary<string, bool>>
            {
                { "name", new Dictionary<string, bool> { { networkName, true } } }
            };

            string networkId;
            var networks = await _dockerClientFixture.DockerClient.Networks.ListNetworksAsync(new NetworksListParameters { Filters = networkFilter }, cancellationToken);
            if (networks.Count > 0)
            {
                networkId = networks.First().ID;
            }
            else
            {
                var networkResult = await _dockerClientFixture.DockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
                {
                    Attachable = true,
                    Name = networkName
                }, cancellationToken);

                networkId = networkResult.ID;
            }

            return networkId;
        }

        private async Task<string> PullAndRunContainerAsync(string containerName, string networkId, string port, CancellationToken cancellationToken)
        {
            var image = "library/docker:dind";
            await _dockerClientFixture.DockerClient.Images.CreateImageAsync(new ImagesCreateParameters
            {
                FromImage = image
            }, new AuthConfig(), new Progress<JSONMessage>(), cancellationToken);

            // Volume CA
            await _dockerClientFixture.DockerClient.Volumes.CreateAsync(new VolumesCreateParameters { Name = "did-certs-ca" });

            // Volume Certs
            await _dockerClientFixture.DockerClient.Volumes.CreateAsync(new VolumesCreateParameters { Name = "did-certs-client" });

            var containerResult = await _dockerClientFixture.DockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
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
                    Privileged = true,
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { "80", new List<PortBinding> { new PortBinding { HostPort = port } } }
                    }
                },
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { "80", new EmptyStruct() }
                },
                AttachStderr = true,
                AttachStdin = true,
                AttachStdout = true,
                Tty = true

            }, cancellationToken);

            await _dockerClientFixture.DockerClient.Networks.ConnectNetworkAsync(networkId, new NetworkConnectParameters
            {
                Container = containerResult.ID
            });

            await _dockerClientFixture.DockerClient.Containers.StartContainerAsync(containerResult.ID, new ContainerStartParameters(), cancellationToken);

            return containerResult.ID;
        }

        private async Task<string> CreateSwarmAsync(string containerId, string networkId, bool createManagerJoinToken, CancellationToken cancellationToken)
        {
            var inspected = await _dockerClientFixture.DockerClient.Networks.InspectNetworkAsync(networkId, cancellationToken);

            var ipAddress = inspected.Containers[containerId].IPv4Address[..^3];

            var execResponse = await _dockerClientFixture.DockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters()
            {
                AttachStderr = true,
                AttachStdout = true,
                Privileged = true,
                Tty = true,
                Cmd = ["docker", "swarm", "init", "--advertise-addr", ipAddress],
            });

            var multiplexedStream = await _dockerClientFixture.DockerClient.Exec.StartAndAttachContainerExecAsync(execResponse.ID, true);
            var response = await multiplexedStream.ReadOutputToEndAsync(cancellationToken);

            if (createManagerJoinToken)
            {
                execResponse = await _dockerClientFixture.DockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters()
                {
                    AttachStderr = true,
                    AttachStdout = true,
                    Privileged = true,
                    Tty = true,
                    Cmd = ["docker", "swarm", "join-token", "manager"],
                });

                multiplexedStream = await _dockerClientFixture.DockerClient.Exec.StartAndAttachContainerExecAsync(execResponse.ID, true);
                response = await multiplexedStream.ReadOutputToEndAsync(cancellationToken);
            }

            var match = Regex.Match(response.stdout, "docker swarm join --token(.*?)(\n)");
            if (match.Success)
            {
                var joinCommand = match.Value.Replace(Environment.NewLine, string.Empty);
                return joinCommand;
            }

            return string.Empty;
        }

        private async Task JoinSwarmAsync(string containerId, string joinCommand, CancellationToken cancellationToken)
        {
            var execResponse = await _dockerClientFixture.DockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters()
            {
                AttachStderr = true,
                AttachStdout = true,
                Privileged = true,
                Tty = true,
                Cmd = joinCommand.Split(" "),
            });

            var multiplexedStream = await _dockerClientFixture.DockerClient.Exec.StartAndAttachContainerExecAsync(execResponse.ID, true);
            await multiplexedStream.ReadOutputToEndAsync(cancellationToken);
        }
    }
}
