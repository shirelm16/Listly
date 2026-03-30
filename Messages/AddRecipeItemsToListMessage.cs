using CommunityToolkit.Mvvm.Messaging.Messages;
using Listly.Model;

namespace Listly.Messages
{
    public class AddRecipeItemsToListMessage : ValueChangedMessage<(Guid ListId, List<ShoppingItemSuggestion> Items)>
    {   
        public AddRecipeItemsToListMessage(Guid listId, List<ShoppingItemSuggestion> items)
            : base((listId, items))
        {
        }
    }
}