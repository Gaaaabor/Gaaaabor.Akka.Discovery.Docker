namespace DockerExample.Cluster
{
    public sealed record SimpleShardEnvelope
    {
        public string? EntityId { get; init; }
        public object? Message { get; init; }
    }
}
