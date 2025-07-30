using CommunityToolkit.Mvvm.Messaging.Messages;
using Listly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Messages
{
    public class ShoppingListUpdatedMessage : ValueChangedMessage<ShoppingList>
    {
        public ShoppingListUpdatedMessage(ShoppingList value) : base(value) { }
    }
}
