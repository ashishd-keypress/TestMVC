using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TestMVC.Models;

namespace Utility
{
    public static class ExpenseStatsHelper
    {
        public static bool TryParseYearMonth(string? monthValue, out int year, out int month)
        {
            year = 0;
            month = 0;
            if (string.IsNullOrWhiteSpace(monthValue)) return false;

            string trimmed = monthValue.Trim();
            if (DateTime.TryParseExact(trimmed, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            {
                year = parsed.Year;
                month = parsed.Month;
                return true;
            }

            return false;
        }

        public static object BuildMonthSummary(List<Expense> allExpenses, int year, int month)
        {
            DateTime now = DateTime.Today;
            DateTime monthDate = new DateTime(year, month, 1);
            var matched = allExpenses
                .Where(e => e.ExpenseDate.Year == year && e.ExpenseDate.Month == month)
                .ToList();
            decimal total = matched.Sum(x => x.Amount);

            return new
            {
                year,
                month,
                monthKey = monthDate.ToString("yyyy-MM"),
                monthName = monthDate.ToString("MMMM yyyy"),
                total,
                totalFormatted = total.ToString("N2"),
                count = matched.Count,
                isCurrentMonth = (year == now.Year && month == now.Month),
                isCompleted = (monthDate.AddMonths(1) <= now)
            };
        }

        public static object BuildStats(List<Expense> allExpenses, int? selectedYear = null, int? selectedMonth = null)
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
                    MonthKey = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
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

            object? selectedSummary = (selectedYear.HasValue && selectedMonth.HasValue)
                ? BuildMonthSummary(allExpenses, selectedYear.Value, selectedMonth.Value)
                : null;

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

                selectedMonth = (selectedYear.HasValue && selectedMonth.HasValue)
                    ? $"{selectedYear.Value:D4}-{selectedMonth.Value:D2}"
                    : null,
                selectedMonthSummary = selectedSummary,

                monthlySummaries = monthlySummaries.Select(m => new
                {
                    year = m.Year,
                    month = m.Month,
                    monthKey = m.MonthKey,
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
