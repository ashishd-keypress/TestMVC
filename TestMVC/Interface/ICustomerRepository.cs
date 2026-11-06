using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetCustomers(string? search = null);
        Task<Customer?> GetCustomerById(long customerId);
        Task<Customer?> GetCustomerByPhone(string phone);
        Task<long> SaveCustomer(Customer customer);
        Task<bool> DeleteCustomer(long customerId);
        Task<int> GetCustomerCount();
        Task<bool> ToggleStatus(long customerId);
    }

    public interface ICustomerService
    {
        Task<List<Customer>> GetCustomers(string? search = null);
        Task<Customer?> GetCustomerById(long customerId);
        Task<Customer?> GetCustomerByPhone(string phone);
        Task<long> SaveCustomer(Customer customer);
        Task<bool> DeleteCustomer(long customerId);
        Task<int> GetCustomerCount();
        Task<bool> ToggleStatus(long customerId);
    }
}
