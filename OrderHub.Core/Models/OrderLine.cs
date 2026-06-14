namespace OrderHub.Core.Models;

public record OrderLine(string Sku, int Quantity, string? Embroidery);
