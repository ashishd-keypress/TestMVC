using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Service
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public Task<List<Customer>> GetCustomers(string? search = null, int? userId = null)
        {
            return _customerRepository.GetCustomers(search, userId);
        }

        public Task<Customer?> GetCustomerById(long customerId, int? userId = null)
        {
            return _customerRepository.GetCustomerById(customerId, userId);
        }

        public Task<Customer?> GetCustomerByPhone(string phone, int? userId = null)
        {
            return _customerRepository.GetCustomerByPhone(phone, userId);
        }

        public Task<long> SaveCustomer(Customer customer)
        {
            return _customerRepository.SaveCustomer(customer);
        }

        public Task<bool> DeleteCustomer(long customerId, int? userId = null)
        {
            return _customerRepository.DeleteCustomer(customerId, userId);
        }

        public Task<bool> ToggleStatus(long customerId, int? userId = null)
        {
            return _customerRepository.ToggleStatus(customerId, userId);
        }

        public Task<int> GetCustomerCount(int? userId = null)
        {
            return _customerRepository.GetCustomerCount(userId);
        }
    }
}
