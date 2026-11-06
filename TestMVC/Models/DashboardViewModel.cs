using System.Collections.Generic;

namespace TestMVC.Models
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalBrands { get; set; }
        public int TotalCategories { get; set; }
        public int TotalInStockDevices { get; set; }
        public decimal TotalStockValue { get; set; }
        public decimal TodaySalesAmount { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalSuppliers { get; set; }
        public List<SalesInvoice> RecentInvoices { get; set; } = new List<SalesInvoice>();
        public List<DeviceUnit> RecentDevices { get; set; } = new List<DeviceUnit>();
    }
}
