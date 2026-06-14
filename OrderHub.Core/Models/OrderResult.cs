namespace OrderHub.Core.Models;

public record OrderResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static OrderResult Success() => new(true);

    public static OrderResult Failure(string error) => new(false, error);
}
