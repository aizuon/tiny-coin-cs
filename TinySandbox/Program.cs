using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Serilog;
using Serilog.Core;
using TinyCoin;
using TinyCoin.BlockChain;
using TinyCoin.Crypto;
using TinyCoin.P2P;
using Log = TinyCoin.Log;

namespace TinySandbox;

public static class Program
{
    private static readonly ILogger Logger = Serilog.Log.ForContext(Constants.SourceContextPropertyName, "Init");

    private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
    {
        var helpText = HelpText.AutoBuild(result, h =>
        {
            h.AdditionalNewLineAfterOption = false;
            return HelpText.DefaultParsingErrorsHandler(result, h);
        }, e => e);
        Console.WriteLine(helpText);
    }

    private static void Main(string[] args)
    {
        Log.Init();

        var parser = new Parser(with => with.HelpWriter = null);
        var parserResult = parser.ParseArguments<Options>(args);

        parserResult.WithParsed(options =>
            {
                if (options.NodeType == "miner")
                {
                    NodeConfig.NodeType = NodeType.Miner;
                }
                else if (options.NodeType == "wallet")
                {
                    NodeConfig.NodeType = NodeType.Wallet;
                }
                else if (options.NodeType == "full")
                {
                    NodeConfig.NodeType = NodeType.Full; //TODO: implement full node
                }
                else
                {
                    Logger.Error("Invalid node type");
                    Environment.Exit(1);
                }

                (byte[] privKey, byte[] _, string address) = options.Wallet != null
                    ? Wallet.InitWallet(options.Wallet)
                    : Wallet.InitWallet();
                NetClient.Init();
                NetClient.ListenAsync(options.Port);

                var pendingConnections = new List<Task>();
                foreach ((string k, ushort v) in NetClient.InitialPeers)
                {
                    if (v == options.Port)
                        continue;

                    pendingConnections.Add(Task.Run(() => NetClient.Connect(k, v)));
                }

                Task.WaitAll(pendingConnections.ToArray());

                if (NodeConfig.NodeType == NodeType.Miner)
                    PoW.MineForever(address);
                else
                    while (true)
                    {
                        string command = Console.ReadLine();

                        if (command == "exit" || command == "quit")
                            break;

                        if (command.StartsWith("address "))
                        {
                            command = command["address ".Length..];
                            Wallet.PrintWalletAddress(command);
                        }
                        else if (command.StartsWith("balance "))
                        {
                            command = command.Substring("balance ".Length);
                            Wallet.PrintBalance(command);
                        }
                        else if (command.StartsWith("balance"))
                        {
                            Wallet.PrintBalance(address);
                        }
                        else if (command.StartsWith("send "))
                        {
                            command = command["send ".Length..];
                            string[] sendArgs = command.Split(' ');
                            if (sendArgs.Length != 2 && sendArgs.Length != 3)
                            {
                                Logger.Error(
                                    "Send command requires 2 arguments, receiver address, send value and optionally fee per byte");

                                continue;
                            }

                            string sendAddress = sendArgs[0];
                            ulong sendValue = ulong.Parse(sendArgs[1]);
                            if (sendArgs.Length == 3)
                            {
                                ulong sendFee = ulong.Parse(sendArgs[2]);
                                Wallet.SendValue(sendValue, sendFee, sendAddress, privKey);
                            }
                            else
                            {
                                Wallet.SendValue(sendValue, 100, sendAddress, privKey);
                            }
                        }
                        else if (command.StartsWith("tx_status "))
                        {
                            command = command["tx_status ".Length..];
                            Wallet.PrintTxStatus(command);
                        }
                        else
                        {
                            Logger.Error("Unknown command");
                        }
                    }

                NetClient.Stop();

                Environment.Exit(0);
            })
            .WithNotParsed(errs => DisplayHelp(parserResult, errs));
    }

    public class Options
    {
        [Option('p', "port", Required = true, HelpText = "Port to listen on network connections")]
        public ushort Port { get; set; }

        [Option('n', "node_type", Required = true, HelpText = "Specify node type")]
        public string NodeType { get; set; }

        [Option('w', "wallet", Required = false, HelpText = "Path to wallet")]
        public string Wallet { get; set; }
    }
}
