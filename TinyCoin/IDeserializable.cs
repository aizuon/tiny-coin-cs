namespace TinyCoin
{
    public interface IDeserializable
    {
        public bool Deserialize(BinaryBuffer buffer);
    }
}
