using System;
using TinyCoin;
using Xunit;

namespace TinyTests;

public class BinaryBufferTests
{
    [Fact]
    public void PrimitiveReadWrite()
    {
        var buffer = new BinaryBuffer();

        bool bReal = true;
        buffer.Write(bReal);
        bool bRead = false;
        Assert.True(buffer.Read(ref bRead));
        Assert.Equal(bReal, bRead);

        byte u8 = 3;
        buffer.Write(u8);
        byte u8Read = 0;
        Assert.True(buffer.Read(ref u8Read));
        Assert.Equal(u8, u8Read);

        sbyte i8 = -5;
        buffer.Write(i8);
        sbyte i8Read = 0;
        Assert.True(buffer.Read(ref i8Read));
        Assert.Equal(i8, i8Read);

        ushort u16 = 10000;
        buffer.Write(u16);
        ushort u16Read = 0;
        Assert.True(buffer.Read(ref u16Read));
        Assert.Equal(u16, u16Read);

        short i16 = -5000;
        buffer.Write(i16);
        short i16Read = 0;
        Assert.True(buffer.Read(ref i16Read));
        Assert.Equal(i16, i16Read);

        uint ui32 = 7000000;
        buffer.Write(ui32);
        uint ui32Read = 0;
        Assert.True(buffer.Read(ref ui32Read));
        Assert.Equal(ui32, ui32Read);

        int i32 = -3000000;
        buffer.Write(i32);
        int i32Read = 0;
        Assert.True(buffer.Read(ref i32Read));
        Assert.Equal(i32, i32Read);

        ulong ui64 = 4000000000;
        buffer.Write(ui64);
        ulong ui64Read = 0;
        Assert.True(buffer.Read(ref ui64Read));
        Assert.Equal(ui64, ui64Read);

        long i64 = -2000000000;
        buffer.Write(i64);
        long i64Read = 0;
        Assert.True(buffer.Read(ref i64Read));
        Assert.Equal(i64, i64Read);
    }

    [Fact]
    public void StringReadWrite()
    {
        var buffer = new BinaryBuffer();

        string str = "foo";
        buffer.Write(str);
        string str_read = string.Empty;
        Assert.True(buffer.Read(ref str_read));
        Assert.Equal(str, str_read);
    }

    [Fact]
    public void VectorReadWrite()
    {
        var buffer = new BinaryBuffer();

        string str = "foo";
        buffer.Write(str);
        string str_read = string.Empty;
        Assert.True(buffer.Read(ref str_read));
        Assert.Equal(str, str_read);

        byte[] u8 = { 3, 5, 7, 9, 11, 55, 75 };
        buffer.Write(u8);
        byte[] u8Read = Array.Empty<byte>();
        Assert.True(buffer.Read(ref u8Read));
        Assert.Equal(u8, u8Read);

        sbyte[] i8 = { -6, -14, -32, -44, -65, -77, -99, -102 };
        buffer.Write(i8);
        sbyte[] i8Read = Array.Empty<sbyte>();
        Assert.True(buffer.Read(ref i8Read));
        Assert.Equal(i8, i8Read);

        ushort[] u16 = { 10000, 20000, 30000, 40000, 50000 };
        buffer.Write(u16);
        ushort[] u16Read = Array.Empty<ushort>();
        Assert.True(buffer.Read(ref u16Read));
        Assert.Equal(u16, u16Read);

        short[] i16 = { -5000, -6000, -7000, -8000, -9000, -10000 };
        buffer.Write(i16);
        short[] i16Read = Array.Empty<short>();
        Assert.True(buffer.Read(ref i16Read));
        Assert.Equal(i16, i16Read);

        uint[] ui32 = { 7000000, 8000000, 9000000 };
        buffer.Write(ui32);
        uint[] ui32Read = Array.Empty<uint>();
        Assert.True(buffer.Read(ref ui32Read));
        Assert.Equal(ui32, ui32Read);

        int[] i32 = { -3000000, -4000000, -5000000 };
        buffer.Write(i32);
        int[] i32Read = Array.Empty<int>();
        Assert.True(buffer.Read(ref i32Read));
        Assert.Equal(i32, i32Read);

        ulong[] ui64 = { 4000000000, 5000000000, 6000000000 };
        buffer.Write(ui64);
        ulong[] ui64Read = Array.Empty<ulong>();
        Assert.True(buffer.Read(ref ui64Read));
        Assert.Equal(ui64, ui64Read);

        long[] i64 = { -2000000000, -5000000000, -8000000000 };
        buffer.Write(i64);
        long[] i64Read = Array.Empty<long>();
        Assert.True(buffer.Read(ref i64Read));
        Assert.Equal(i64, i64Read);
    }
}
