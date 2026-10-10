using System;
using System.Threading.Tasks;
using TestMVC.Models.ReqRes;

namespace TestMVC.Interface
{
    public interface IDashboardService
    {
        Task<Tuple<GetDashboardStatsRes, int, string>> GetDashboardStats(int? userId = null);
    }
}
