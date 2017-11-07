using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace net.fex.api.v1
{
    public class BaseConnection : IConnection
    {
        public bool IsSignedIn { get { return this.UserSignedIn != null; } }

        public User UserSignedIn { get; protected set; }


        public virtual void Dispose()
        {
        }

        public virtual User SignIn(string login, string password, bool stay_signed)
        {
            this.UserSignedIn = new User(login, 0);
            return this.UserSignedIn;
        }

        public async Task<User> SignInAsync(string login, string password, bool stay_signed)
        {
            return await Task.Run<User>(() => { return this.SignIn(login, password, stay_signed); });
        }

        public virtual void SignOut()
        {
            this.UserSignedIn = null;
        }

        public async Task SignOutAsync()
        {
            await Task.Run(() => { this.SignOut(); });
        }
    }
}
