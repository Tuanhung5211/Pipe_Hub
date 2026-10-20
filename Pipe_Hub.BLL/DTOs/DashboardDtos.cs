namespace Pipe_Hub.BLL.DTOs;

/// <summary>Doanh thu theo tháng — dùng cho BarChart Dashboard.</summary>
public record RevenueByMonthDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal TotalRevenue { get; init; }
    public string Label => $"{Month:D2}/{Year}";
}

/// <summary>Số người chơi truy cập (CCU) theo ngày — dùng cho LineChart Dashboard.</summary>
public record CcuByDayDto
{
    public DateTime Date { get; init; }
    public int UniquePlayers { get; init; }
    public string Label => Date.ToString("dd/MM");
}

/// <summary>Số game theo thể loại — dùng cho PieChart Dashboard.</summary>
public record GameCountByGenreDto
{
    public string GenreName { get; init; } = string.Empty;
    public int GameCount { get; init; }
}
