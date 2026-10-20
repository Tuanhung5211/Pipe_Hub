using Microsoft.Data.SqlClient;

namespace Pipe_Hub.DAL.AdoNet;

/// <summary>
/// Đối chiếu EF Core: tính doanh thu 12 tháng gần nhất bằng ADO.NET thuần.
/// </summary>
public class RevenueAdoRepository : IRevenueAdoRepository
{
    private readonly string _connectionString;

    public RevenueAdoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<(int Year, int Month, decimal TotalRevenue)>> GetRevenueByMonthAsync(int months = 12)
    {
        var result = new List<(int Year, int Month, decimal TotalRevenue)>();

        const string sql = @"
            SELECT TOP (@Months)
                YEAR(CreatedAt) AS [Year],
                MONTH(CreatedAt) AS [Month],
                SUM(TotalAmount) AS [TotalRevenue]
            FROM Orders
            WHERE Status = 1 /* OrderStatus.Completed */
            GROUP BY YEAR(CreatedAt), MONTH(CreatedAt)
            ORDER BY [Year] DESC, [Month] DESC;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Months", months);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var year = reader.GetInt32(0);
            var month = reader.GetInt32(1);
            var total = reader.GetDecimal(2);
            result.Add((year, month, total));
        }

        return result;
    }
}
