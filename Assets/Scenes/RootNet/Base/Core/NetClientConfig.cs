namespace RootNet
{
    public sealed class NetClientConfig
    {
        public float TimeoutSeconds { get; set; } = 15.0f;
        public bool UseTimeout { get; set; } = true;
    }
}