using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using TestMVC.Interface;
using TestMVC.Repository;
using TestMVC.Service;

namespace TestMVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "TestMVC.Auth";
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddAuthorization();

            // Register Repositories
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IBrandRepository, BrandRepository>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IDeviceUnitRepository, DeviceUnitRepository>();
            builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            builder.Services.AddScoped<ISalesRepository, SalesRepository>();
            builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();

            // Register Services
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IBrandService, BrandService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IDeviceUnitService, DeviceUnitService>();
            builder.Services.AddScoped<ISupplierService, SupplierService>();
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<IPurchaseService, PurchaseService>();
            builder.Services.AddScoped<ISalesService, SalesService>();
            builder.Services.AddScoped<IExpenseService, ExpenseService>();

            var app = builder.Build();

            // Ensure database schema and multi-tenant columns exist
            var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                Utility.DbInitializer.EnsureDatabaseSetup(connectionString);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            // Map API Controllers (Attribute Routing e.g., /api/Auth/login, /api/Product/toggle-status)
            app.MapControllers();

            // Map MVC Default Route - Opens directly on Login page when app starts
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
