using System;

namespace RootNet
{
    public struct NetReader
    {
        private readonly byte[] _buffer;
        private readonly int _end;
        private int _position;

        public NetReader(ArraySegment<byte> data)
        {
            _buffer = data.Array ?? throw new ArgumentNullException(nameof(data));
            _position = data.Offset;
            _end = data.Offset + data.Count;
        }

        public byte ReadByte()
        {
            EnsureReadable(1);
            return _buffer[_position++];
        }

        public ushort ReadUInt16()
        {
            EnsureReadable(2);
            ushort value = (ushort)(_buffer[_position] | (_buffer[_position + 1] << 8));
            _position += 2;
            return value;
        }

        public int ReadInt32()
        {
            EnsureReadable(4);
            int value =
                _buffer[_position] |
                (_buffer[_position + 1] << 8) |
                (_buffer[_position + 2] << 16) |
                (_buffer[_position + 3] << 24);
            _position += 4;
            return value;
        }

        public float ReadFloat()
        {
            EnsureReadable(4);
            float value = BitConverter.ToSingle(_buffer, _position);
            _position += 4;
            return value;
        }

        private void EnsureReadable(int size)
        {
            if (_position + size > _end)
                throw new InvalidOperationException("NetReader overflow.");
        }
    }
}