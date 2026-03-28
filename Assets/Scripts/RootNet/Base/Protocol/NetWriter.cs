using System;

namespace RootNet
{
    public struct NetWriter
    {
        private byte[] _buffer;
        private int _position;

        public NetWriter(int capacity)
        {
            _buffer = new byte[capacity];
            _position = 0;
        }

        public void WriteByte(byte value)
        {
            EnsureCapacity(1);
            _buffer[_position++] = value;
        }

        public void WriteUInt16(ushort value)
        {
            EnsureCapacity(2);
            _buffer[_position++] = (byte)(value & 0xFF);
            _buffer[_position++] = (byte)((value >> 8) & 0xFF);
        }

        public void WriteInt32(int value)
        {
            EnsureCapacity(4);
            _buffer[_position++] = (byte)(value & 0xFF);
            _buffer[_position++] = (byte)((value >> 8) & 0xFF);
            _buffer[_position++] = (byte)((value >> 16) & 0xFF);
            _buffer[_position++] = (byte)((value >> 24) & 0xFF);
        }

        public void WriteFloat(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            EnsureCapacity(4);
            Buffer.BlockCopy(bytes, 0, _buffer, _position, 4);
            _position += 4;
        }

        public ArraySegment<byte> ToArraySegment()
        {
            byte[] copy = new byte[_position];
            Buffer.BlockCopy(_buffer, 0, copy, 0, _position);
            return new ArraySegment<byte>(copy);
        }

        private void EnsureCapacity(int add)
        {
            int required = _position + add;
            if (_buffer.Length >= required)
                return;

            int newSize = _buffer.Length * 2;
            if (newSize < required)
                newSize = required;

            Array.Resize(ref _buffer, newSize);
        }
    }
}