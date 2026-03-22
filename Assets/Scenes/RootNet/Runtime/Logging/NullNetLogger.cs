namespace RootNet
{
    public sealed class NullNetLogger : INetLogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }
}