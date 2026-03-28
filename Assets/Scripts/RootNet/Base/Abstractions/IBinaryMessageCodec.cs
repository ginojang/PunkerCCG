namespace RootNet
{
    public interface IBinaryMessageCodec<T> where T : IBinaryMessage
    {
        void Write(ref NetWriter writer, T message);
        T Read(ref NetReader reader);
    }
}