namespace TinyCoin;

public static class NetParams
{
    public const uint MaxBlockSerializedSizeInBytes = 1000000;

    public const byte CoinbaseMaturity = 2;

    public const uint MaxFutureBlockTimeInSecs = 60 * 60 * 2;

    public const ulong Coin = 100000000;
    public const ulong TotalCoins = 21000000;
    public const ulong MaxMoney = Coin * TotalCoins;

    public const uint TimeBetweenBlocksInSecsTarget = 60 * 10;
    public const uint DifficultyPeriodInSecsTarget = 60 * 60 * 24;

    public const uint DifficultyPeriodInBlocks = DifficultyPeriodInSecsTarget /
                                                 TimeBetweenBlocksInSecsTarget;

    public const byte InitialDifficultyBits = 24;
    public const uint HalveSubsidyAfterBlocksNum = 210000;
}
