using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetCustomers(string? search = null, int? userId = null);
        Task<Customer?> GetCustomerById(long customerId, int? userId = null);
        Task<Customer?> GetCustomerByPhone(string phone, int? userId = null);
        Task<long> SaveCustomer(Customer customer);
        Task<bool> DeleteCustomer(long customerId, int? userId = null);
        Task<int> GetCustomerCount(int? userId = null);
        Task<bool> ToggleStatus(long customerId, int? userId = null);
    }

    public interface ICustomerService
    {
        Task<List<Customer>> GetCustomers(string? search = null, int? userId = null);
        Task<Customer?> GetCustomerById(long customerId, int? userId = null);
        Task<Customer?> GetCustomerByPhone(string phone, int? userId = null);
        Task<long> SaveCustomer(Customer customer);
        Task<bool> DeleteCustomer(long customerId, int? userId = null);
        Task<int> GetCustomerCount(int? userId = null);
        Task<bool> ToggleStatus(long customerId, int? userId = null);
    }
}
