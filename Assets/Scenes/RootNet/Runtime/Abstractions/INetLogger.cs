namespace RootNet
{
    public interface INetLogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}