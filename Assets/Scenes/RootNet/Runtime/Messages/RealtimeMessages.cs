namespace RootNet.Messages
{
    public struct MoveInputMessage : IBinaryMessage
    {
        public ushort MessageId => 1001;

        public ushort Tick;
        public float MoveX;
        public float MoveY;
        public byte Buttons;
    }

    public sealed class MoveInputMessageCodec : IBinaryMessageCodec<MoveInputMessage>
    {
        public void Write(ref NetWriter writer, MoveInputMessage message)
        {
            writer.WriteUInt16(message.Tick);
            writer.WriteFloat(message.MoveX);
            writer.WriteFloat(message.MoveY);
            writer.WriteByte(message.Buttons);
        }

        public MoveInputMessage Read(ref NetReader reader)
        {
            MoveInputMessage msg;
            msg.Tick = reader.ReadUInt16();
            msg.MoveX = reader.ReadFloat();
            msg.MoveY = reader.ReadFloat();
            msg.Buttons = reader.ReadByte();
            return msg;
        }
    }


    public struct AttackInputMessage : IBinaryMessage
    {
        public ushort MessageId => 1002;

        public ushort Tick;
        public int SkillId;
        public int TargetId;
    }

    public sealed class AttackInputMessageCodec : IBinaryMessageCodec<AttackInputMessage>
    {
        public void Write(ref NetWriter writer, AttackInputMessage message)
        {
            writer.WriteUInt16(message.Tick);
            writer.WriteInt32(message.SkillId);
            writer.WriteInt32(message.TargetId);
        }

        public AttackInputMessage Read(ref NetReader reader)
        {
            AttackInputMessage msg;
            msg.Tick = reader.ReadUInt16();
            msg.SkillId = reader.ReadInt32();
            msg.TargetId = reader.ReadInt32();
            return msg;
        }
    }
}