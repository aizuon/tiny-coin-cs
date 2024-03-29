﻿namespace TinyCoin;

public interface IDeserializable<out T>
{
    public static abstract T Deserialize(BinaryBuffer buffer);
}
