﻿using Neo.IO.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuildTransaction
{
    public class RpcResponse
    {
        public JObject Id { get; set; }

        public string JsonRpc { get; set; }

        public RpcResponseError Error { get; set; }

        public JObject Result { get; set; }

        public string RawResponse { get; set; }

        public static RpcResponse FromJson(JObject json)
        {
            var response = new RpcResponse
            {
                Id = json["id"],
                JsonRpc = json["jsonrpc"].AsString(),
                Result = json["result"]
            };

            if (json["error"] != null)
            {
                response.Error = RpcResponseError.FromJson(json["error"]);
            }

            return response;
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["id"] = Id;
            json["jsonrpc"] = JsonRpc;
            json["error"] = Error?.ToJson();
            json["result"] = Result;
            return json;
        }
    }

    public class RpcResponseError
    {
        public int Code { get; set; }

        public string Message { get; set; }

        public JObject Data { get; set; }

        public static RpcResponseError FromJson(JObject json)
        {
            return new RpcResponseError
            {
                Code = (int)json["code"].AsNumber(),
                Message = json["message"].AsString(),
                Data = json["data"],
            };
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["code"] = Code;
            json["message"] = Message;
            json["data"] = Data;
            return json;
        }
    }
}
