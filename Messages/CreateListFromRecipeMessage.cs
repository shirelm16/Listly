using CommunityToolkit.Mvvm.Messaging.Messages;
using Listly.Model;

namespace Listly.Messages
{
    public class CreateListFromRecipeMessage : ValueChangedMessage<(string ListName, List<ShoppingItemSuggestion> Items)>
    {
        public CreateListFromRecipeMessage(string listName, List<ShoppingItemSuggestion> items)
            :base((listName, items))
        {
        }
    }
}