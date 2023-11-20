namespace TinyCoin;

public interface ICloneable<out T> : ISerializable, IDeserializable<T>
{
}

public static class CloneableExtensions
{
    public static T Clone<T>(this T obj) where T : ICloneable<T>
    {
        return T.Deserialize(obj.Serialize());
    }
}
