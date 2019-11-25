namespace MindCraft.Common.Serialization
{
    public interface IBinarySerializable
    {
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
    }
}