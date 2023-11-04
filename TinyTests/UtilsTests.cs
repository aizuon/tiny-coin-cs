using TinyCoin;
using Xunit;

namespace TinyTests;

public class UtilsTests
{
    [Fact]
    public void ByteArrayToHexString()
    {
        string hexString = Utils.ByteArrayToHexString(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 });

        Assert.Equal("0001020304", hexString);
    }

    [Fact]
    public void HexStringToByteArray()
    {
        byte[] byteArray = Utils.HexStringToByteArray("0001020304");

        byte[] assertedByteArray = { 0x00, 0x01, 0x02, 0x03, 0x04 };

        Assert.Equal(byteArray, assertedByteArray);
    }

    [Fact]
    public void StringToByteArray()
    {
        byte[] byteArray = Utils.StringToByteArray("foo");

        byte[] assertedByteArray = Utils.HexStringToByteArray("666F6F");

        Assert.Equal(byteArray, assertedByteArray);
    }
}
