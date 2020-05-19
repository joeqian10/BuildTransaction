using Neo;
using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BuildTransaction
{
    /// <summary>
    /// The RPC client to call NEO RPC methods
    /// </summary>
    public class RpcClient : IDisposable
    {
        private HttpClient httpClient;

        public RpcClient(string url, string rpcUser = default, string rpcPass = default)
        {
            httpClient = new HttpClient() { BaseAddress = new Uri(url) };
            if (!string.IsNullOrEmpty(rpcUser) && !string.IsNullOrEmpty(rpcPass))
            {
                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{rpcUser}:{rpcPass}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
            }
        }

        public RpcClient(HttpClient client)
        {
            httpClient = client;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    httpClient?.Dispose();
                }

                httpClient = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public async Task<RpcResponse> SendAsync(RpcRequest request)
        {
            var requestJson = request.ToJson().ToString();
            using var result = await httpClient.PostAsync(httpClient.BaseAddress, new StringContent(requestJson, Encoding.UTF8));
            var content = await result.Content.ReadAsStringAsync();
            var response = RpcResponse.FromJson(JObject.Parse(content));
            response.RawResponse = content;

            if (response.Error != null)
            {
                throw new RpcException(response.Error.Code, response.Error.Message);
            }

            return response;
        }

        public RpcResponse Send(RpcRequest request)
        {
            try
            {
                return SendAsync(request).Result;
            }
            catch (AggregateException ex)
            {
                throw ex.GetBaseException();
            }
        }

        public virtual JObject RpcSend(string method, params JObject[] paraArgs)
        {
            var request = new RpcRequest
            {
                Id = 1,
                JsonRpc = "2.0",
                Method = method,
                Params = paraArgs
            };
            return Send(request).Result;
        }

        #region Blockchain

        /// <summary>
        /// Returns the hash of the tallest block in the main chain.
        /// </summary>
        public string GetBestBlockHash()
        {
            return RpcSend("getbestblockhash").AsString();
        }

        /// <summary>
        /// Returns the hash of the tallest block in the main chain.
        /// The serialized information of the block is returned, represented by a hexadecimal string.
        /// </summary>
        public string GetBlockHex(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return RpcSend("getblock", index).AsString();
            }
            return RpcSend("getblock", hashOrIndex).AsString();
        }

        

        /// <summary>
        /// Gets the number of blocks in the main chain.
        /// </summary>
        public uint GetBlockCount()
        {
            return (uint)RpcSend("getblockcount").AsNumber();
        }

        /// <summary>
        /// Returns the hash value of the corresponding block, based on the specified index.
        /// </summary>
        public string GetBlockHash(int index)
        {
            return RpcSend("getblockhash", index).AsString();
        }

        /// <summary>
        /// Returns the corresponding block header information according to the specified script hash.
        /// </summary>
        public string GetBlockHeaderHex(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return RpcSend("getblockheader", index).AsString();
            }
            return RpcSend("getblockheader", hashOrIndex).AsString();
        }

       

       

       

        /// <summary>
        /// Obtains the list of unconfirmed transactions in memory.
        /// </summary>
        public string[] GetRawMempool()
        {
            return ((JArray)RpcSend("getrawmempool")).Select(p => p.AsString()).ToArray();
        }


        /// <summary>
        /// Returns the corresponding transaction information, based on the specified hash value.
        /// </summary>
        public string GetRawTransactionHex(string txHash)
        {
            return RpcSend("getrawtransaction", txHash).AsString();
        }

        

        /// <summary>
        /// Returns the stored value, according to the contract script hash (or Id) and the stored key.
        /// </summary>
        public string GetStorage(string scriptHashOrId, string key)
        {
            if (int.TryParse(scriptHashOrId, out int id))
            {
                return RpcSend("getstorage", id, key).AsString();
            }

            return RpcSend("getstorage", scriptHashOrId, key).AsString();
        }

        /// <summary>
        /// Returns the block index in which the transaction is found.
        /// </summary>
        public uint GetTransactionHeight(string txHash)
        {
            return uint.Parse(RpcSend("gettransactionheight", txHash).AsString());
        }


        #endregion Blockchain

        #region Node

        /// <summary>
        /// Gets the current number of connections for the node.
        /// </summary>
        public int GetConnectionCount()
        {
            return (int)RpcSend("getconnectioncount").AsNumber();
        }

        

        /// <summary>
        /// Broadcasts a serialized transaction over the NEO network.
        /// </summary>
        public bool SendRawTransaction(byte[] rawTransaction)
        {
            return bool.Parse(RpcSend("sendrawtransaction", rawTransaction.ToHexString()).AsString());
        }

        /// <summary>
        /// Broadcasts a transaction over the NEO network.
        /// </summary>
        public bool SendRawTransaction(Transaction transaction)
        {
            return SendRawTransaction(transaction.ToArray());
        }

        /// <summary>
        /// Broadcasts a serialized block over the NEO network.
        /// </summary>
        public UInt256 SubmitBlock(byte[] block)
        {
            return UInt256.Parse(RpcSend("submitblock", block.ToHexString())["hash"].AsString());
        }

        #endregion Node

        #region SmartContract

        /// <summary>
        /// Returns the result after calling a smart contract at scripthash with the given operation and parameters.
        /// This RPC call does not affect the blockchain in any way.
        /// </summary>
        public RpcInvokeResult InvokeFunction(string scriptHash, string operation, RpcStack[] stacks, params UInt160[] scriptHashesForVerifying)
        {
            List<JObject> parameters = new List<JObject> { scriptHash, operation, stacks.Select(p => p.ToJson()).ToArray() };
            if (scriptHashesForVerifying.Length > 0)
            {
                parameters.Add(scriptHashesForVerifying.Select(p => (JObject)p.ToString()).ToArray());
            }
            return RpcInvokeResult.FromJson(RpcSend("invokefunction", parameters.ToArray()));
        }

        /// <summary>
        /// Returns the result after passing a script through the VM.
        /// This RPC call does not affect the blockchain in any way.
        /// </summary>
        public RpcInvokeResult InvokeScript(byte[] script, params UInt160[] scriptHashesForVerifying)
        {
            List<JObject> parameters = new List<JObject> { script.ToHexString() };
            if (scriptHashesForVerifying.Length > 0)
            {
                parameters.Add(scriptHashesForVerifying.Select(p => (JObject)p.ToString()).ToArray());
            }
            return RpcInvokeResult.FromJson(RpcSend("invokescript", parameters.ToArray()));
        }

        #endregion SmartContract

       

        #region Wallet

        

        /// <summary>
        /// Exports the private key of the specified address.
        /// </summary>
        public string DumpPrivKey(string address)
        {
            return RpcSend("dumpprivkey", address).AsString();
        }

        

        /// <summary>
        /// Creates a new account in the wallet opened by RPC.
        /// </summary>
        public string GetNewAddress()
        {
            return RpcSend("getnewaddress").AsString();
        }

        /// <summary>
        /// Gets the amount of unclaimed GAS in the wallet.
        /// </summary>
        public BigInteger GetUnclaimedGas()
        {
            return BigInteger.Parse(RpcSend("getunclaimedgas").AsString());
        }

        public RpcUnspent GetUnspents(string address)
        {
            return RpcUnspent.FromJson(RpcSend("getunspents", address));
        }

        

        /// <summary>
        /// Open wallet file in the provider's machine.
        /// By default, this method is disabled by RpcServer config.json.
        /// </summary>
        public bool OpenWallet(string path, string password)
        {
            return RpcSend("openwallet", path, password).AsBoolean();
        }

        

        #endregion Utilities

        

    }
}
