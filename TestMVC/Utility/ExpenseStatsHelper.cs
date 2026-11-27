using System;
using System.Collections.Generic;
using System.Linq;
using TestMVC.Models;

namespace Utility
{
    public static class ExpenseStatsHelper
    {
        public static object BuildStats(List<Expense> allExpenses)
        {
            DateTime now = DateTime.Today;

            var currentMonthExpenses = allExpenses
                .Where(e => e.ExpenseDate.Year == now.Year && e.ExpenseDate.Month == now.Month)
                .ToList();
            decimal currentMonthTotal = currentMonthExpenses.Sum(e => e.Amount);
            int currentMonthCount = currentMonthExpenses.Count;
            string currentMonthName = now.ToString("MMMM yyyy");

            var monthlySummaries = allExpenses
                .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthDate = new DateTime(g.Key.Year, g.Key.Month, 1),
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                    Total = g.Sum(x => x.Amount),
                    Count = g.Count(),
                    IsCurrentMonth = (g.Key.Year == now.Year && g.Key.Month == now.Month),
                    IsCompleted = (new DateTime(g.Key.Year, g.Key.Month, 1).AddMonths(1) <= now)
                })
                .OrderByDescending(m => m.MonthDate)
                .ToList();

            var completedMonths = monthlySummaries.Where(m => m.IsCompleted).ToList();
            decimal completedMonthsTotal = completedMonths.Sum(m => m.Total);
            decimal allTimeTotal = allExpenses.Sum(e => e.Amount);
            int allTimeCount = allExpenses.Count;

            return new
            {
                currentMonthTotal,
                currentMonthTotalFormatted = currentMonthTotal.ToString("N2"),
                currentMonthCount,
                currentMonthName,

                completedMonthsTotal,
                completedMonthsTotalFormatted = completedMonthsTotal.ToString("N2"),
                completedMonthsCount = completedMonths.Count,

                allTimeTotal,
                allTimeTotalFormatted = allTimeTotal.ToString("N2"),
                allTimeCount,

                monthlySummaries = monthlySummaries.Select(m => new
                {
                    year = m.Year,
                    month = m.Month,
                    monthName = m.MonthName,
                    total = m.Total,
                    totalFormatted = m.Total.ToString("N2"),
                    count = m.Count,
                    isCurrentMonth = m.IsCurrentMonth,
                    isCompleted = m.IsCompleted
                }).ToList()
            };
        }
    }
}
