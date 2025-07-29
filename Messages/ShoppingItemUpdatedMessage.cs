using CommunityToolkit.Mvvm.Messaging.Messages;
using Listly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Messages
{
    internal class ShoppingItemUpdatedMessage : ValueChangedMessage<ShoppingItem>
    {
        public ShoppingItemUpdatedMessage(ShoppingItem value) : base(value) { }
    }
}
