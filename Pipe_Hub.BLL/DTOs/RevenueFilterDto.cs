namespace Pipe_Hub.BLL.DTOs;

/// <summary>Bộ lọc cho báo cáo doanh thu chi tiết.</summary>
public record RevenueFilterDto
{
    /// <summary>Ngày bắt đầu (null = không giới hạn).</summary>
    public DateTime? FromDate { get; init; }

    /// <summary>Ngày kết thúc (null = không giới hạn).</summary>
    public DateTime? ToDate { get; init; }

    /// <summary>Tổng tiền tối thiểu (null = không giới hạn).</summary>
    public decimal? MinAmount { get; init; }

    /// <summary>Tổng tiền tối đa (null = không giới hạn).</summary>
    public decimal? MaxAmount { get; init; }
}
