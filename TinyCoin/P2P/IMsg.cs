namespace TinyCoin.P2P;

public interface IMsg<out T> : IHandleable, ISerializable, IDeserializable<T>, ICloneable<T>
{
}
