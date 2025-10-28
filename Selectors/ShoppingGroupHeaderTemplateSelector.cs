using Listly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Selectors;

public class ShoppingGroupHeaderTemplateSelector : DataTemplateSelector
{
    public DataTemplate ExpandableHeaderTemplate { get; set; }
    public DataTemplate EmptyHeaderTemplate { get; set; }

    public ShoppingGroupHeaderTemplateSelector() { }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ShoppingItemsGroup group)
        {
            return group.IsExpandable ? ExpandableHeaderTemplate : EmptyHeaderTemplate;
        }
        return null;
    }
}
