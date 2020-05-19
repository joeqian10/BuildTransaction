using System;
using System.Collections.Generic;
using System.Text;

namespace BuildTransaction
{
    public class RpcException : Exception
    {
        public RpcException(int code, string message) : base(message)
        {
            HResult = code;
        }
    }
}
