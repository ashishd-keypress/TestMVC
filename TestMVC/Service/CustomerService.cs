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

        public Task<List<Customer>> GetCustomers(string? search = null)
        {
            return _customerRepository.GetCustomers(search);
        }

        public Task<Customer?> GetCustomerById(long customerId)
        {
            return _customerRepository.GetCustomerById(customerId);
        }

        public Task<Customer?> GetCustomerByPhone(string phone)
        {
            return _customerRepository.GetCustomerByPhone(phone);
        }

        public Task<long> SaveCustomer(Customer customer)
        {
            return _customerRepository.SaveCustomer(customer);
        }

        public Task<bool> DeleteCustomer(long customerId)
        {
            return _customerRepository.DeleteCustomer(customerId);
        }

        public Task<bool> ToggleStatus(long customerId)
        {
            return _customerRepository.ToggleStatus(customerId);
        }

        public Task<int> GetCustomerCount()
        {
            return _customerRepository.GetCustomerCount();
        }
    }
}
