using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Models;
using OrderHub.Core.Services;
using Xunit;

namespace OrderHub.Tests.Services;

public class OrderProcessingServiceTests
{
    [Fact]
    public async Task ProcessOrder_WhenItemIsOutOfStock_FailsAndDoesNotChargePayment()
    {
        // ---------------------------------------------------------
        // 1. ARRANGE (Set up the test data and our "fake" services)
        // ---------------------------------------------------------
        var schoolId = 1;
        var sku = "BLAZER-01";
        var parentEmail = "parent@test.com";

        // The parent wants 5 blazers.
        var request = new OrderRequest(
            schoolId,
            new List<OrderLine> { new(sku, Quantity: 5, Embroidery: null) },
            parentEmail);

        // But the database says we only have 2 in stock.
        var fakeProductsDb = new Dictionary<string, ProductDetails>
        {
            { sku, new ProductDetails(sku, BasePrice: 50.00m, StockQuantity: 2) }
        };

        var mockSchoolRepo = new Mock<ISchoolRepository>();
        mockSchoolRepo.Setup(x => x.GetSchoolTierAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("STANDARD");

        var mockProductRepo = new Mock<IProductRepository>();
        mockProductRepo.Setup(x => x.GetProductsBySkusAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeProductsDb);

        var mockPaymentGateway = new Mock<IPaymentGateway>();
        var mockQueue = new Mock<IConfirmationQueue>();
        var mockLogger = new Mock<ILogger<OrderProcessingService>>();

        var sut = new OrderProcessingService(
            mockSchoolRepo.Object,
            mockProductRepo.Object,
            mockPaymentGateway.Object,
            mockQueue.Object,
            mockLogger.Object);

        // ---------------------------------------------------------
        // 2. ACT (Run the actual business logic)
        // ---------------------------------------------------------
        var result = await sut.ProcessOrderAsync(request, CancellationToken.None);

        // ---------------------------------------------------------
        // 3. ASSERT (Verify the results are exactly what we expect)
        // ---------------------------------------------------------
        Assert.False(result.IsSuccess);
        Assert.Equal($"FAIL: out of stock {sku}", result.ErrorMessage);

        // CRITICAL BUSINESS RULE: payment gateway must never be called.
        mockPaymentGateway.Verify(
            x => x.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
