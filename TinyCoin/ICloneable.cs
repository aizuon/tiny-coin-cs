namespace TinyCoin;

public interface ICloneable<out T>
{
    T Clone();
}

public static class CloneableExtensions
{
    public static T Clone<T>(this T obj) where T : ICloneable<T>
    {
        return obj.Clone();
    }
}
