using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebTabanliStajTakipSistemi.Data;
using WebTabanliStajTakipSistemi.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebTabanliStajTakipSistemi.Controllers
{
	public class AccountController : Controller
	{
		private readonly AppDbContext _context;

		public AccountController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public IActionResult Login() => View();

		[HttpPost]
		public async Task<IActionResult> Login(string number, string password, string role)
		{
			// Seçilen role göre ilgili tabloda arama yapıyoruz
			if (role == "Student")
			{
				var student = await _context.Students
					.FirstOrDefaultAsync(u => u.StudentNumber == number && u.Password == password && !u.IsDeleted);

				if (student != null) return await DoLogin(student.FullName, student.Email, "Student", student.Id.ToString());
			}
			else if (role == "Academic")
			{
				var academic = await _context.Academics
					.FirstOrDefaultAsync(u => u.AcademicNumber.ToString() == number && u.Password == password && !u.IsDeleted);

				if (academic != null) return await DoLogin(academic.FullName, academic.Email, "Academic", academic.Id.ToString());
			}
			else if (role == "Admin")
			{
				var admin = await _context.Admins
					.FirstOrDefaultAsync(u => u.Email == number && u.PasswordHash == password); // Admin genelde email ile girer

				if (admin != null) return await DoLogin(admin.FullName, admin.Email, "Admin", admin.Id.ToString());
			}

			ViewBag.Hata = "Giriş başarısız! Bilgilerinizi kontrol ediniz.";
			return View();
		}

		// Tekrarlanan Login işlemini merkezi bir metoda aldık
		private async Task<IActionResult> DoLogin(string name, string email, string role, string id)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.Name, name),
				new Claim(ClaimTypes.Email, email),
				new Claim(ClaimTypes.Role, role),
				new Claim("UserId", id)
			};

			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

			// Role göre yönlendirme yapılabilir
			if (role == "Admin") return RedirectToAction("Index", "Admin");
			return RedirectToAction("Index", "Home");
		}

		[HttpGet]
		public async Task<IActionResult> Register()
		{
			// İngilizce model isimlerine (Department/University) göre dropdownları dolduruyoruz
			ViewBag.UniversityId = new SelectList(await _context.Universities.ToListAsync(), "Id", "UniversityName");
			ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "DepartmentName");
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(Student student)
		{
			if (ModelState.IsValid)
			{
				// IsDeleted ve CreatedDate zaten BaseEntity'den default değerlerle geliyor.
				_context.Students.Add(student);
				await _context.SaveChangesAsync();
				return RedirectToAction("Login");
			}

			ViewBag.UniversityId = new SelectList(await _context.Universities.ToListAsync(), "Id", "UniversityName");
			ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "DepartmentName");
			return View(student);
		}

		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Login");
		}
	}
}