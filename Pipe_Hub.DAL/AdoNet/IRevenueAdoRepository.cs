using System.Data;

namespace Pipe_Hub.DAL.AdoNet;

public interface IRevenueAdoRepository
{
    /// <summary>
    /// Lấy doanh thu theo tháng (Order Completed) trong N tháng gần nhất.
    /// Trả về tuple (Year, Month, TotalRevenue), sắp xếp giảm dần.
    /// </summary>
    Task<List<(int Year, int Month, decimal TotalRevenue)>> GetRevenueByMonthAsync(int months = 12);
}
