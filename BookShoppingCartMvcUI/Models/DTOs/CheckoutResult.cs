namespace BookShoppingCartMvcUI.Models.DTOs;

public enum CheckoutFailure
{
    NotLoggedIn,
    EmptyCart,
    InvalidQuantity,
    OutOfStock,
    MissingPendingStatus,
    // the database threw, details are in the log
    Error
}

// what DoCheckout did. OrderId is set when it worked, Failure + ErrorMessage when it didn't.
// ErrorMessage is ok to show the user
public record CheckoutResult(bool Succeeded, int? OrderId, CheckoutFailure? Failure, string? ErrorMessage)
{
    public static CheckoutResult Success(int orderId) => new(true, orderId, null, null);

    public static CheckoutResult Failed(CheckoutFailure failure, string message) => new(false, null, failure, message);
}
