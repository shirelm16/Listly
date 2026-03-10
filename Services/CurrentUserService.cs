using Firebase.Firestore.Auth;
using Plugin.Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Services
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        bool IsAnonymous { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IFirebaseAuth _auth;

        public CurrentUserService(IFirebaseAuth auth)
        {
            _auth = auth;
        }

        public string? UserId => _auth.CurrentUser?.Uid;

        public bool IsAnonymous => _auth.CurrentUser.IsAnonymous;
    }
}
