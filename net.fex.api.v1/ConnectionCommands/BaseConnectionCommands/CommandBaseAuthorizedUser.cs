﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Fex.Api
{
    public abstract class CommandBaseAuthorizedUser : CommandBase
    {
        public CommandBaseAuthorizedUser()
        {
        }

        public CommandBaseAuthorizedUser(IDictionary<string, string> parameters) : base(parameters)
        {
        }

        public override bool CanExecute(IConnection connection)
        {
            return connection.IsSignedIn;
        }
    }
}
