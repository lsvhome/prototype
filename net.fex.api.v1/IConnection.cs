using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Net.Fex.Api
{
    public interface IConnection : IDisposable
    {
        Uri Endpoint { get; }

        System.Net.Http.HttpClient Client { get; }

        bool IsSignedIn { get; }

        CommandSignIn.User UserSignedIn { get; }

        CommandSignIn.User SignIn(string login, string password, bool stay_signed);

        Task<CommandSignIn.User> SignInAsync(string login, string password, bool stay_signed);

        void SignOut();

        Task SignOutAsync();

        bool LoginCheck(string login);

        Task<bool> LoginCheckAsync(string login);
    }
}
