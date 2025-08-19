using Firebase.Auth;
using Plugin.Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Service
{
    public interface IAuthService
    {
        Task<string> GetCurrentUserIdAsync();
        bool IsAuthenticated();
        Task SignInAnonymouslyAsync();
    }

    public class FirebaseAuthService : IAuthService
    {
        public async Task<string> GetCurrentUserIdAsync()
        {
            var user = CrossFirebaseAuth.Current.CurrentUser;
            if (user == null)
            {
                await SignInAnonymouslyAsync();
                user = CrossFirebaseAuth.Current.CurrentUser;
            }
            return user?.Uid ?? string.Empty;
        }

        public bool IsAuthenticated()
        {
            return CrossFirebaseAuth.Current.CurrentUser != null;
        }

        public async Task SignInAnonymouslyAsync()
        {
            await CrossFirebaseAuth.Current.SignInAnonymouslyAsync();
        }
    }
}
