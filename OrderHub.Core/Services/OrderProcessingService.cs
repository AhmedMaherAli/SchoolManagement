using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Models;

namespace OrderHub.Core.Services;

/// <summary>
/// Modern order orchestrator utilizing .NET 8 primary constructors.
/// </summary>
public class OrderProcessingService(
    ISchoolRepository schoolRepo,
    IProductRepository productRepo,
    IPaymentGateway paymentGateway,
    IConfirmationQueue confirmationQueue,
    ILogger<OrderProcessingService> logger)
{
    private const string GoldTier = "GOLD";
    private const string SilverTier = "SILVER";
    private const decimal GoldDiscountMultiplier = 0.85m;
    private const decimal SilverDiscountMultiplier = 0.92m;
    private const int ShortEmbroideryMaxLength = 3;
    private const decimal ShortEmbroiderySurcharge = 4.50m;
    private const decimal LongEmbroiderySurcharge = 8.00m;

    public async Task<OrderResult> ProcessOrderAsync(
        OrderRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch school tier.
        var tier = await schoolRepo.GetSchoolTierAsync(request.SchoolId, cancellationToken);
        if (string.IsNullOrEmpty(tier))
        {
            logger.LogWarning("Order failed: School {SchoolId} not found.", request.SchoolId);
            return OrderResult.Failure("FAIL: school not found");
        }

        // 2. Resolve N+1: fetch all required products in one database call.
        var skus = request.Lines.Select(line => line.Sku).Distinct();
        var products = await productRepo.GetProductsBySkusAsync(skus, cancellationToken);

        decimal subtotal = 0;

        // 3. Process lines, pricing, and inventory.
        foreach (var line in request.Lines)
        {
            if (!products.TryGetValue(line.Sku, out var product))
            {
                return OrderResult.Failure($"FAIL: invalid SKU {line.Sku}");
            }

            if (product.StockQuantity < line.Quantity)
            {
                return OrderResult.Failure($"FAIL: out of stock {line.Sku}");
            }

            var price = CalculateDiscountedPrice(product.BasePrice, tier);
            price += CalculateEmbroiderySurcharge(line.Embroidery);

            subtotal += price * line.Quantity;
        }

        // 4. Handle payment via injected gateway abstraction.
        var paymentSuccess = await paymentGateway.ProcessPaymentAsync(
            subtotal,
            request.ParentEmail,
            cancellationToken);

        if (!paymentSuccess)
        {
            logger.LogWarning("Payment failed for email {Email}", request.ParentEmail);
            return OrderResult.Failure("FAIL: payment");
        }

        // 5. Offload confirmation email to background queue.
        await confirmationQueue.EnqueueEmailAsync(request.ParentEmail, subtotal, cancellationToken);

        logger.LogInformation("Order successfully processed for {Email}", request.ParentEmail);
        return OrderResult.Success();
    }

    // Pure business rule function for deterministic pricing logic.
    private static decimal CalculateDiscountedPrice(decimal basePrice, string tier) =>
        tier switch
        {
            GoldTier => basePrice * GoldDiscountMultiplier,
            SilverTier => basePrice * SilverDiscountMultiplier,
            _ => basePrice
        };

    private static decimal CalculateEmbroiderySurcharge(string? embroidery)
    {
        if (string.IsNullOrEmpty(embroidery))
        {
            return 0;
        }

        return embroidery.Length <= ShortEmbroideryMaxLength
            ? ShortEmbroiderySurcharge
            : LongEmbroiderySurcharge;
    }
}
