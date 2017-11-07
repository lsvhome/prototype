using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace net.fex.api.v1
{
    public interface IConnection : IDisposable
    {
        bool IsSignedIn { get; }

        User UserSignedIn { get; }

        User SignIn(string login, string password, bool stay_signed);
        Task<User> SignInAsync(string login, string password, bool stay_signed);

        void SignOut();
        Task SignOutAsync();
    }
}
