namespace TinyCoin.P2P;

public interface IHandleable
{
    public void Handle(Connection con);

    public static abstract OpCode GetOpCode();
}
