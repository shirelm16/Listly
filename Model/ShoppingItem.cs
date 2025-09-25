using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Listly.Model
{
    public partial class ShoppingItem : ObservableObject
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        [Indexed]
        public Guid ShoppingListId { get; set; }

        [Ignore]
        public string AddedBy { get; set; }

        [ObservableProperty]
        string name;

        [ObservableProperty]
        double? quantity;

        [ObservableProperty]
        string? unit;

        [ObservableProperty]
        bool isPurchased;

        [ObservableProperty]
        ItemCategory category = new ItemCategory();

        public ShoppingItem() { }

        public ShoppingItem(Guid shoppingListId, string name, double? quantity, string? unit, ItemCategory? category)
        {
            Id = Guid.NewGuid();
            ShoppingListId = shoppingListId;
            Name = name;
            Quantity = quantity;
            Unit = unit;
            Category = category;
        }
        
        public event Action<ShoppingItem>? ItemPurchased;

        public event Action<ShoppingItem>? ItemUnpurchased;

        public event Action<ShoppingItem>? CategoryChanged;

        partial void OnIsPurchasedChanged(bool value)
        {
            if (value)
            {
                ItemPurchased?.Invoke(this);
            }
            else
            {
                ItemUnpurchased?.Invoke(this);
            }
        }

        partial void OnCategoryChanged(ItemCategory? oldValue, ItemCategory newValue)
        {
            CategoryChanged?.Invoke(this);
        }
    }

    public enum Category
    {
        Bakery,
        Beverages,
        CansAndJars,
        CleaningAndLaundry,
        Clothing,
        CoffeeAndTea,
        DairyAndEggs,
        DryGoods,
        Electronics,
        Frozen,
        FruitAndVegetables,
        Health,
        MeatAndSeafood,
        Other,
        PersonalCare,
        Pets,
        SnacksAndSweets,
        SpicesSaucesAndOils
    }

    public class ItemCategory
    {
        public Category Name { get; set; }
        public bool IsBuiltIn { get; set; } = true;

        public ItemCategory()
        {
            Name = Category.Other;
        }

        public ItemCategory(Category category)
        {
            Name = category;
        }
    }

    public static class CategoryHelper
    {
        private static readonly Dictionary<Category, (string DisplayName, string Icon)> _categoryInfo = new()
        {
            { Category.Bakery, ("Bakery", "🥖") },
            { Category.Bakery, ("Beverages", "🥛") },
            { Category.CansAndJars, ("Cans & Jars", "🥫") },
            { Category.CleaningAndLaundry, ("Cleaning & Laundry", "🧼") },
            { Category.Clothing, ("Clothing", "👕") },
            { Category.CoffeeAndTea, ("Coffee & Tea", "☕️") },
            { Category.DairyAndEggs, ("Dairy & Eggs", "🧀") },
            { Category.DairyAndEggs, ("Dry Goods", "🌾") },
            { Category.Electronics, ("Electronics", "📺") },
            { Category.Frozen, ("Frozen", "❄️") },
            { Category.FruitAndVegetables, ("Fruit & Vegetables", "🍅") },
            { Category.Health, ("Health", "💊") },
            { Category.MeatAndSeafood, ("Meat & Seafood", "🥩") },
            { Category.Other, ("Other", "🏷️") },
            { Category.PersonalCare, ("Personal Care", "🧴") },
            { Category.Pets, ("Pets", "🐾") },
            { Category.SnacksAndSweets, ("Snacks & Sweets", "🍬") },
            { Category.SpicesSaucesAndOils, ("Spices, Sauces & Oils", "🧂") },
        };

        public static string GetDisplayName(this Category category) => _categoryInfo[category].DisplayName;
        public static string GetIcon(this Category category) => _categoryInfo[category].Icon;
        public static string GetDisplayWithIcon(this Category category) =>
            $"{_categoryInfo[category].Icon} {_categoryInfo[category].DisplayName}";
        public static Category FromDisplayNameAndIcon(string displayNameAndIcon) => _displayAndIconToEnum[displayNameAndIcon];
        public static Category FromDisplayName(string displayName) => _displayToEnum[displayName];

        private static readonly Dictionary<string, Category> _displayAndIconToEnum =
            _categoryInfo.ToDictionary(kvp => $"{kvp.Value.Icon} {kvp.Value.DisplayName}", kvp => kvp.Key);

        private static readonly Dictionary<string, Category> _displayToEnum =
            _categoryInfo.ToDictionary(kvp => kvp.Value.DisplayName, kvp => kvp.Key);
    }
}