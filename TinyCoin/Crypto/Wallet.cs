using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Serilog;
using Serilog.Core;
using TinyCoin.BlockChain;
using TinyCoin.P2P;
using TinyCoin.P2P.Messages;
using TinyCoin.Txs;
using UTXO = TinyCoin.Txs.UnspentTxOut;

namespace TinyCoin.Crypto;

public static class Wallet
{
    private const char PubKeyHashVersion = '1';
    private const string DefaultWalletPath = "wallet.dat";
    private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(Wallet));
    private static string _walletPath = DefaultWalletPath;
    private static bool _printedAddress;

    public static string PubKeyToAddress(byte[] pubKey)
    {
        byte[] sha256 = SHA256.HashBinary(pubKey);
        byte[] ripemd160 = RIPEMD160.HashBinary(sha256);
        byte[] ripemd160WithVersionByte = Utils.HexStringToByteArray($"00{Utils.ByteArrayToHexString(ripemd160)}");
        byte[] sha256d = SHA256.DoubleHashBinary(ripemd160WithVersionByte);
        byte[] checksum = sha256d[..4];
        byte[] binaryAddress =
            Utils.HexStringToByteArray(
                $"{Utils.ByteArrayToHexString(ripemd160WithVersionByte)}{Utils.ByteArrayToHexString(checksum)}");
        string address = Base58.Encode(binaryAddress);
        return $"{PubKeyHashVersion}{address}";
    }

    public static (byte[], byte[], string) GetWallet(string walletPath)
    {
        byte[] privKey;
        byte[] pubKey;
        string address;

        if (File.Exists(walletPath))
        {
            using (var walletIn = new FileStream(walletPath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(walletIn))
                {
                    privKey = reader.ReadBytes((int)reader.BaseStream.Length);
                    pubKey = ECDSA.GetPubKeyFromPrivKey(privKey);
                    address = PubKeyToAddress(pubKey);
                }
            }
        }
        else
        {
            Logger.Information("Generating new wallet {}", walletPath);

            var keys = ECDSA.Generate();
            privKey = keys.Item1;
            pubKey = keys.Item2;
            address = PubKeyToAddress(pubKey);

            using (var walletOut = new FileStream(walletPath, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new BinaryWriter(walletOut))
                {
                    writer.Write(privKey);
                }
            }
        }

        return (privKey, pubKey, address);
    }

    public static void PrintWalletAddress(string walletPath)
    {
        (_, _, string address) = GetWallet(walletPath);

        Logger.Information("Wallet {} belongs to address {}", walletPath, address);
    }

    public static (byte[], byte[], string) InitWallet(string walletPath)
    {
        _walletPath = walletPath;

        (byte[] privKey, byte[] pubKey, string address) = GetWallet(_walletPath);

        if (!_printedAddress)
        {
            _printedAddress = true;

            Logger.Information("Your address is {}", address);
        }

        return (privKey, pubKey, address);
    }

    public static (byte[], byte[], string) InitWallet()
    {
        return InitWallet(_walletPath);
    }

    public static TxIn BuildTxIn(byte[] privKey, TxOutPoint txOutPoint, IList<TxOut> txOuts)
    {
        const int sequence = -1;

        byte[] pubKey = ECDSA.GetPubKeyFromPrivKey(privKey);
        byte[] spendMsg = MsgSerializer.BuildSpendMsg(txOutPoint, pubKey, sequence, txOuts);
        byte[] unlockSig = ECDSA.SignMsg(spendMsg, privKey);

        return new TxIn(txOutPoint, unlockSig, pubKey, sequence);
    }

    public static Tx SendValue_Miner(ulong value, ulong fee, string address, byte[] privKey)
    {
        var tx = BuildTx_Miner(value, fee, address, privKey);
        if (tx == null)
            return null;
        Logger.Information("Built transaction {}, adding to MemPool", tx.Id());
        MemPool.AddTxToMemPool(tx);
        NetClient.SendMsgRandom(new TxInfoMsg(tx));

        return tx;
    }

    public static Tx SendValue(ulong value, ulong fee, string address, byte[] privKey)
    {
        var tx = BuildTx(value, fee, address, privKey);
        if (tx == null)
            return null;
        Logger.Information("Built transaction {}, broadcasting", tx.Id());
        if (!NetClient.SendMsgRandom(new TxInfoMsg(tx)))
            Logger.Error("No connection to send transaction");

        return tx;
    }

    public static TxStatusResponse GetTxStatus_Miner(string tx_id)
    {
        var ret = new TxStatusResponse();

        lock (MemPool.Mutex)
        {
            foreach (string tx in MemPool.Map.Keys)
                if (tx == tx_id)
                {
                    ret.Status = TxStatus.MemPool;

                    return ret;
                }
        }

        lock (Chain.Mutex)
        {
            for (uint height = 0; height < Chain.ActiveChain.Count; height++)
            {
                var block = Chain.ActiveChain[(int)height];
                foreach (var tx in block.Txs)
                    if (tx.Id() == tx_id)
                    {
                        ret.Status = TxStatus.Mined;
                        ret.BlockId = block.Id();
                        ret.BlockHeight = height;

                        return ret;
                    }
            }
        }

        ret.Status = TxStatus.NotFound;

        return ret;
    }

    public static TxStatusResponse GetTxStatus(string tx_id)
    {
        var ret = new TxStatusResponse();

        if (MsgCache.SendMemPoolMsg != null)
            MsgCache.SendMemPoolMsg = null;

        if (!NetClient.SendMsgRandom(new GetMemPoolMsg()))
        {
            Logger.Error("No connection to ask MemPool");

            return ret;
        }

        long start = Utils.GetUnixTimestamp();
        while (MsgCache.SendMemPoolMsg == null)
        {
            if (Utils.GetUnixTimestamp() - start > MsgCache.MaxMsgAwaitTimeInSecs)
            {
                Logger.Error("Timeout on GetMemPoolMsg");

                return ret;
            }

            Thread.Sleep(16);
        }

        foreach (string tx in MsgCache.SendMemPoolMsg.MemPool)
            if (tx == tx_id)
            {
                ret.Status = TxStatus.MemPool;

                return ret;
            }

        if (MsgCache.SendActiveChainMsg != null)
            MsgCache.SendActiveChainMsg = null;

        if (!NetClient.SendMsgRandom(new GetActiveChainMsg()))
        {
            Logger.Error("No connection to ask active chain");

            return ret;
        }

        start = Utils.GetUnixTimestamp();
        while (MsgCache.SendActiveChainMsg == null)
        {
            if (Utils.GetUnixTimestamp() - start > MsgCache.MaxMsgAwaitTimeInSecs)
            {
                Logger.Error("Timeout on GetActiveChainMsg");

                return ret;
            }

            Thread.Sleep(16);
        }

        for (uint height = 0; height < MsgCache.SendActiveChainMsg.ActiveChain.Count; height++)
        {
            var block = MsgCache.SendActiveChainMsg.ActiveChain[(int)height];
            foreach (var tx in block.Txs)
                if (tx.Id() == tx_id)
                {
                    ret.Status = TxStatus.Mined;
                    ret.BlockId = block.Id();
                    ret.BlockHeight = height;

                    return ret;
                }
        }

        ret.Status = TxStatus.NotFound;

        return ret;
    }

    public static void PrintTxStatus(string txId)
    {
        var response = GetTxStatus(txId);
        switch (response.Status)
        {
            case TxStatus.MemPool:
            {
                Logger.Information("Transaction {} is in MemPool", txId);

                break;
            }
            case TxStatus.Mined:
            {
                Logger.Information("Transaction {} is mined in {} at height {}", txId, response.BlockId,
                    response.BlockHeight);

                break;
            }
            case TxStatus.NotFound:
            {
                Logger.Information("Transaction {} not found", txId);

                break;
            }
        }
    }

    public static ulong GetBalance_Miner(string address)
    {
        var utxos = FindUTXOsForAddress_Miner(address);
        ulong value = 0;
        foreach (var utxo in utxos)
            value += utxo.TxOut.Value;

        return value;
    }

    public static ulong GetBalance(string address)
    {
        var utxos = FindUTXOsForAddress(address);
        ulong value = 0;
        foreach (var utxo in utxos)
            value += utxo.TxOut.Value;

        return value;
    }

    public static void PrintBalance(string address)
    {
        ulong balance = GetBalance(address);
        Logger.Information("Address {} holds {} coins", address, balance);
    }

    private static Tx BuildTxFromUTXOs(IList<UTXO> utxos, ulong value, ulong fee,
        string address, string changeAddress,
        byte[] privKey)
    {
        var utxosList = utxos.ToList();
        utxosList.Sort((a, b) => a.TxOut.Value.CompareTo(b.TxOut.Value));
        utxosList.Sort((a, b) => a.Height.CompareTo(b.Height));
        var selectedUtxos = new HashSet<UTXO>();
        ulong inSum = 0;
        uint totalSizeEst = 300;
        ulong totalFeeEst = totalSizeEst * fee;
        foreach (var coin in utxosList)
        {
            selectedUtxos.Add(coin);
            foreach (var selectedCoin in selectedUtxos)
                inSum += selectedCoin.TxOut.Value;
            if (inSum <= value + totalFeeEst)
                inSum = 0;
            else
                break;
        }

        if (inSum == 0)
        {
            Logger.Error("Not enough coins");

            return null;
        }

        var txOut = new TxOut(value, address);
        ulong change = inSum - value - totalFeeEst;
        var txOutChange = new TxOut(change, changeAddress);
        var txOuts = new List<TxOut> { txOut, txOutChange };
        var txIns = new List<TxIn>(selectedUtxos.Count);
        foreach (var selectedCoin in selectedUtxos)
            txIns.Add(BuildTxIn(privKey, selectedCoin.TxOutPoint, txOuts));
        var tx = new Tx(txIns, txOuts, 0);
        uint txSize = (uint)tx.Serialize().Buffer.Length;
        uint realFee = (uint)(totalFeeEst / txSize);
        Logger.Information("Built transaction {} with {} coins/byte fee", tx.Id(), realFee);
        return tx;
    }

    private static Tx BuildTx_Miner(ulong value, ulong fee, string address,
        byte[] privKey)
    {
        byte[] pubKey = ECDSA.GetPubKeyFromPrivKey(privKey);
        string myAddress = PubKeyToAddress(pubKey);
        var myCoins = FindUTXOsForAddress_Miner(myAddress);
        if (myCoins.Count == 0)
        {
            Logger.Error("No coins found");

            return null;
        }

        return BuildTxFromUTXOs(myCoins, value, fee, address, myAddress, privKey);
    }

    private static Tx BuildTx(ulong value, ulong fee, string address, byte[] privKey)
    {
        byte[] pubKey = ECDSA.GetPubKeyFromPrivKey(privKey);
        string myAddress = PubKeyToAddress(pubKey);
        var myCoins = FindUTXOsForAddress(myAddress);
        if (myCoins.Count == 0)
        {
            Logger.Error("No coins found");

            return null;
        }

        return BuildTxFromUTXOs(myCoins, value, fee, address, myAddress, privKey);
    }

    private static IList<UTXO> FindUTXOsForAddress_Miner(string address)
    {
        var utxos = new List<UTXO>();
        {
            lock (UTXO.Mutex)
            {
                foreach (var v in UTXO.Map.Values)
                    if (v.TxOut.ToAddress == address)
                        utxos.Add(v);
            }
        }
        return utxos;
    }

    private static IList<UTXO> FindUTXOsForAddress(string address)
    {
        if (MsgCache.SendUTXOsMsg != null)
            MsgCache.SendUTXOsMsg = null;

        if (!NetClient.SendMsgRandom(new GetUTXOsMsg()))
        {
            Logger.Error("No connection to ask UTXO set");

            return new List<UTXO>();
        }

        long start = Utils.GetUnixTimestamp();
        while (MsgCache.SendUTXOsMsg == null)
        {
            if (Utils.GetUnixTimestamp() - start > MsgCache.MaxMsgAwaitTimeInSecs)
            {
                Logger.Error("Timeout on GetUTXOsMsg");

                return new List<UTXO>();
            }

            Thread.Sleep(16);
        }

        var utxos = new List<UTXO>();
        foreach (var v in MsgCache.SendUTXOsMsg.UTXOs.Values)
            if (v.TxOut.ToAddress == address)
                utxos.Add(v);
        return utxos;
    }

    public class TxStatusResponse
    {
        public long BlockHeight = -1;
        public string BlockId = string.Empty;
        public TxStatus Status = TxStatus.NotFound;
    }
}
