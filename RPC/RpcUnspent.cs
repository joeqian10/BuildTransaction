using Neo;
using Neo.IO.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildTransaction
{
    public class RpcUnspent
    {
        public UnspentBalance[] Balances { get; set; }
        public string Address { get; set; }

        public static RpcUnspent FromJson(JObject json)
        {
            return new RpcUnspent
            {
                Balances = ((JArray)json["balance"]).Select(p => UnspentBalance.FromJson(p)).ToArray(),
                Address = json["address"].AsString()
            };
        }
    }

    public class UnspentBalance
    {
        public Unspent[] Unspents { get; set; }
        public string AssetHash { get; set; }
        public string Asset { get; set; }
        public string AssetSymbol { get; set; }
        public Fixed8 Amount { get; set; }

        public static UnspentBalance FromJson(JObject json)
        {
            return new UnspentBalance
            {
                Unspents = ((JArray)json["unspent"]).Select(p => Unspent.FromJson(p)).ToArray(),
                AssetHash = json["asset_hash"].AsString(),
                Asset = json["asset"].AsString(),
                AssetSymbol = json["asset_symbol"].AsString(),
                Amount = Fixed8.FromDecimal(decimal.Parse(json["amount"].AsString()))
            };
        }
    }

    public class Unspent
    {
        public string TxId { get; set; }
        public int N { get; set; }
        public Fixed8 Value { get; set; }

        public static Unspent FromJson(JObject json)
        {
            return new Unspent
            {
                TxId = json["txid"].AsString(),
                N = int.Parse(json["n"].AsString()),
                Value = Fixed8.FromDecimal(decimal.Parse(json["value"].AsString()))
            };
        }
    }
}
