using Firebase.Auth;
using Plugin.Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Service
{
    public class FirebaseAuthService
    {
        private readonly IFirebaseAuth _auth;

        public FirebaseAuthService(IFirebaseAuth auth)
        {
            _auth = auth;
        }

        public async Task<IFirebaseUser> SignInAnonymouslyAsync()
        {
            var result = await _auth.SignInAnonymouslyAsync();
            return result;
        }

        public IFirebaseUser CurrentUser => _auth.CurrentUser;
    }
}
