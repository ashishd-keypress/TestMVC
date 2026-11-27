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
            // Ensure ContentRootPath resolves to the project folder containing Views and wwwroot,
            // regardless of whether launched via Visual Studio, IIS Express, Solution root, or dotnet CLI.
            var contentRoot = Directory.GetCurrentDirectory();
            if (!Directory.Exists(Path.Combine(contentRoot, "Views")))
            {
                var subFolder = Path.Combine(contentRoot, "TestMVC");
                if (Directory.Exists(Path.Combine(subFolder, "Views")))
                {
                    contentRoot = subFolder;
                }
                else
                {
                    var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
                    if (Directory.Exists(Path.Combine(baseDir, "Views")))
                    {
                        contentRoot = baseDir;
                    }
                }
            }

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = contentRoot
            });

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

            app.UseMiddleware<Utility.ApiObservabilityMiddleware>();

            app.MapStaticAssets();

            // Map API Controllers (Attribute Routing e.g., /api/Auth/login, /api/Product/toggle-status)
            app.MapControllers();

            // Map MVC Default Route - "/" resolves to Home/Index (Dashboard; [Authorize] redirects unauthenticated users to /Account/Login)
            // and /{Controller} cleanly resolves to /{Controller}/Index
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
