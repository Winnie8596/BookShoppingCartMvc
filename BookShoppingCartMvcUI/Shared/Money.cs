using System.Globalization;

namespace BookShoppingCartMvcUI.Shared;

// price formatting in one place: "RM1,234.50"
public static class Money
{
    public static string Format(decimal amount) => "RM" + amount.ToString("N2", CultureInfo.InvariantCulture);
}
