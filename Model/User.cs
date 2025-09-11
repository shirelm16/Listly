using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Model
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public List<string> DeviceTokens { get; set; }  = new List<string>();

        public User() { }
        public User(string uid, string deviceToken)
        {
            Id = uid;
            DeviceTokens.Add(deviceToken);
        }
    }
}
