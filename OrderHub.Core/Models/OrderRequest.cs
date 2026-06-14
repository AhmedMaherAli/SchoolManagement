using System.Collections.Generic;

namespace OrderHub.Core.Models;

public record OrderRequest(int SchoolId, List<OrderLine> Lines, string ParentEmail);
