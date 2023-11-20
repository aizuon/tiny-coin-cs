namespace TinyCoin.P2P;

public interface IHandleable
{
    void Handle(Connection con);

    OpCode GetOpCode();
}
