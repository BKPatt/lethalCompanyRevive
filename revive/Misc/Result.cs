using System;
using System.Collections.Generic;
using System.Text;

namespace lethalCompanyRevive.Misc
{
    public readonly ref struct Result
    {
        public bool Success { get; }
        public string? Message { get; }

        public Result(bool success, string? message = null)
        {
            Success = success;
            Message = message;
        }

        public Result(string message) : this(false, message)
        {
        }
    }

}

