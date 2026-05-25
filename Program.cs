using Microsoft.EntityFrameworkCore;
using WebTabanliStajTakipSistemi.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebTabanliStajTakipSistemi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
			// Add services to the container.
			builder.Services.AddControllersWithViews();

			builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	            .AddCookie(options =>
	            {
		            options.LoginPath = "/Account/Login"; // Giriþ yapmamýþ birini buraya atar
		            options.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisi olmayaný buraya atar
		            options.Cookie.Name = "StajSistemiCookie";
	            });

			var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
				pattern: "{controller=Student}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
