using Neo;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace BuildTransaction
{
    class Program
    {
        private static RpcClient rpcClient;
        private static string nep2Key; 

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // those you need to set up------------------------------------------------------------

            rpcClient = new RpcClient("http://localhost:20002"); // your node url, such as "http://47.89.240.111:12332"
            nep2Key = "6PYLjXkQzADs7r36XQjByJXoggm3beRh6UzxuN59NZiBxFBnm1HPvv3ytM"; // you can find this in your wallet, the "key" field
            string password = "1"; // your password

            UInt160 from = "AQzRMe3zyGS8W177xLJfewRRQZY2kddMun".ToScriptHash(); // your account address, such as "APPmjituYcgfNxjuQDy9vP73R2PmhFsYJR"
            UInt160 scriptHash = UInt160.Parse("0x6b4f6926c28523519c758ec2015a60ddfe8b37dc"); // your contract script hash, such as "0x5be5fc0641e44b0003262b3fda775ea60133cb05"
            //byte[] param = new byte[] { }; // your contract parameter
            BigInteger param = 2;

            //-------------------------------------------------------------------------------------------

            ScriptBuilder sb = new ScriptBuilder().EmitAppCall(scriptHash, "getProxyHash", new ContractParameter[] { new ContractParameter() { Type = ContractParameterType.Integer, Value = param } });
            var script = sb.ToArray();

            InvocationTransaction itx = new InvocationTransaction();
            itx.Inputs = new CoinReference[] { };
            itx.Outputs = new TransactionOutput[] { };
            itx.Attributes = new TransactionAttribute[] { };
            itx.Witnesses = new Witness[] { };
            itx.Version = 1;
            itx.Script = script;
            itx.Gas = GetGasConsumed(script);

            Fixed8 fee = itx.Gas;

            if (itx.Size > 1024)
            {
                fee += Fixed8.FromDecimal(0.001m);
                fee += Fixed8.FromDecimal(itx.Size * 0.00001m);
            }

            var (inputs, sum) = GetTransactionInputs(from, Blockchain.UtilityToken.Hash, fee);
            if (sum > fee)
                itx.Outputs = itx.Outputs.Concat(new[] { new TransactionOutput() { AssetId = Blockchain.UtilityToken.Hash, Value = sum - fee, ScriptHash = from } }).ToArray();
            itx.Inputs = itx.Inputs.Concat(inputs).ToArray();

            // sign the itx
            Random random = new Random();
            var nonce = new byte[32];
            random.NextBytes(nonce);
            TransactionAttribute[] attributes = new TransactionAttribute[] 
            { 
                new TransactionAttribute() { Usage = TransactionAttributeUsage.Script, Data = from.ToArray() },
                new TransactionAttribute() { Usage = TransactionAttributeUsage.Remark1, Data = nonce } // if a transaction has no inputs and outputs, need to add nonce for duplication
            };
            itx.Attributes = itx.Attributes.Concat(attributes).ToArray();

            KeyPair keyPair = WalletHelper.KeyPairFromNep2(nep2Key, password);
            Witness witness = WalletHelper.CreateTransactionWitness(itx, keyPair);
            itx.Witnesses = itx.Witnesses.Concat(new[] { witness }).ToArray();

            var raw = itx.ToArray();
            var txid = rpcClient.SendRawTransaction(raw);
            Console.WriteLine(txid.ToString());

        }

        private static Fixed8 GetGasConsumed(byte[] script)
        {
            RpcInvokeResult response = rpcClient.InvokeScript(script);
            if (Fixed8.TryParse(response.GasConsumed, out Fixed8 result))
            {
                Fixed8 gas = result - Fixed8.FromDecimal(10);
                if (gas <= Fixed8.Zero)
                {
                    return Fixed8.Zero;
                }
                else
                {
                    return gas.Ceiling();
                }
            }
            throw new Exception();
        }

        private static (CoinReference[], Fixed8) GetTransactionInputs(UInt160 from, UInt256 assetId, Fixed8 amount)
        {
            UnspentBalance unspentBalance;
            Fixed8 available;
            (unspentBalance, available) = GetBalance(from, assetId);

            if (available < amount)
            {
                throw new Exception("Insufficient funds");
            }

            var unspents = unspentBalance.Unspents;
            var unspents_ordered = unspents.OrderByDescending(p => p.Value).ToArray();
            int i = 0;
            Fixed8 sum = Fixed8.Zero;
            while (unspents_ordered[i].Value <= amount)
                amount -= unspents_ordered[i++].Value;
            if (amount == Fixed8.Zero)
            {
                return (unspents_ordered.Take(i).Select(p => UnspentToCoinReferenece(p)).ToArray(), unspents_ordered.Take(i).Sum(p => p.Value));
            }
            else
                return (unspents_ordered.Take(i).Concat(new[] { unspents_ordered.Last(p => p.Value >= amount) }).Select(p => UnspentToCoinReferenece(p)).ToArray(), 
                    unspents_ordered.Take(i).Concat(new[] { unspents_ordered.Last(p => p.Value >= amount) }).Sum(p => p.Value));
        }

        private static CoinReference UnspentToCoinReferenece(Unspent unspent)
        {
            return new CoinReference()
            {
                PrevHash = UInt256.Parse(unspent.TxId),
                PrevIndex = (ushort)unspent.N
            };
        }

        private static (UnspentBalance, Fixed8) GetBalance(UInt160 account, UInt256 assetId)
        {
            var response = rpcClient.GetUnspents(account.ToAddress());
            var balances = response.Balances;
            foreach (var balance in balances)
            {
                if ("0x" + balance.AssetHash == assetId.ToString())
                {
                    return (balance, balance.Amount);
                }
            }
            throw new Exception("Asset not found");
        }
    }
}
