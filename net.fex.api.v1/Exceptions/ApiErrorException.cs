using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Fex.Api
{
    public class ApiErrorException : ConnectionException
    {
        public ApiErrorException(CommandBase.ResponseResultFail responseResult) : base(responseResult.Error.Message)
        {
            this.ResponseResult = responseResult;
        }

        public CommandBase.ResponseResultFail ResponseResult { get; private set; }
    }
}
