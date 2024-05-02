using Docker.DotNet;

namespace Gaaaabor.Akka.Discovery.Docker.Tests
{
    public class DockerClientFixture : IDisposable
    {
        private bool disposedValue;
        public DockerClient DockerClient { get; }

        public DockerClientFixture()
        {
            var dockerClientConfiguration = new DockerClientConfiguration();
            DockerClient = dockerClientConfiguration.CreateClient();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    DockerClient.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
