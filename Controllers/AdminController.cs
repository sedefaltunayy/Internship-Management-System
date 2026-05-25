using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebTabanliStajTakipSistemi.Data;
using WebTabanliStajTakipSistemi.Enums;
using WebTabanliStajTakipSistemi.Models;
using WebTabanliStajTakipSistemi.ViewModels;

namespace WebTabanliStajTakipSistemi.Controllers
{
	[Authorize(Roles = "Admin")]
	public class AdminController : Controller
	{
		private readonly AppDbContext _context;

		public AdminController(AppDbContext context)
		{
			_context = context;
		}

		// DASHBOARD: Genel İstatistikler ve Son Kayıtlar
		public async Task<IActionResult> Index()
		{
			ViewBag.OgrenciSayisi = await _context.Students.CountAsync(s => !s.IsDeleted);

			// ÇÖZÜM: Enum karşılaştırması bu şekilde yapılır
			ViewBag.BekleyenBasvuruSayisi = await _context.Internships
				.CountAsync(i => i.Status == InternshipStatus.Pending && !i.IsDeleted);

			var ayarlar = await _context.SystemSettings.FirstOrDefaultAsync();
			ViewBag.DonemAcikMi = ayarlar?.IsPeriod ?? false;

			ViewBag.SonOgrenciler = await _context.Students
				.Include(s => s.Department)
				.Where(s => !s.IsDeleted)
				.OrderByDescending(s => s.CreatedDate)
				.Take(5)
				.ToListAsync();

			ViewBag.SonUniversiteler = await _context.Universities
				.Where(u => !u.IsDeleted)
				.Take(5)
				.ToListAsync();

			return View();
		}

		// LOGIN (GET)
		[AllowAnonymous]
		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		// LOGIN (POST)
		[AllowAnonymous]
		[HttpPost]
		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<IActionResult> Login(string email, string sifre)
		{
			var admin = await _context.Admins
				.FirstOrDefaultAsync(x => x.Email == email && x.PasswordHash == sifre && !x.IsDeleted);

			if (admin == null)
			{
				ViewBag.Error = "E-posta veya şifre hatalı.";
				return View();
			}

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
				new Claim(ClaimTypes.Name, admin.FullName),
				new Claim(ClaimTypes.Email, admin.Email),
				new Claim(ClaimTypes.Role, "Admin")
			};

			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var principal = new ClaimsPrincipal(identity);

			await HttpContext.SignInAsync(
				CookieAuthenticationDefaults.AuthenticationScheme,
				principal,
				new AuthenticationProperties
				{
					IsPersistent = true
				});

			return RedirectToAction("Index", "Admin");
		}

		// ÖĞRENCİ LİSTESİ
		public async Task<IActionResult> Students()
		{
			var students = await _context.Students
				.Include(s => s.Department)
				.Where(s => !s.IsDeleted)
				.ToListAsync();
			return View(students);
		}

		// AKADEMİSYEN LİSTESİ
		public async Task<IActionResult> Teachers()
		{
			var academics = await _context.Academics
				.Include(a => a.Department)
				.Where(a => !a.IsDeleted)
				.ToListAsync();
			return View(academics);
		}

		// YÖNETİCİ LİSTESİ
		public async Task<IActionResult> Admins()
		{
			var admins = await _context.Admins
				.Where(a => !a.IsDeleted)
				.ToListAsync();
			return View(admins);
		}

		// FİRMALARI LİSTELE
		public async Task<IActionResult> Companies()
		{
			var companies = await _context.Companies
				.Where(c => !c.IsDeleted)
				.ToListAsync();
			return View(companies);
		}

		// BÖLÜMLERİ LİSTELE
		public async Task<IActionResult> Departments()
		{
			var departments = await _context.Departments
				.Include(d => d.University)
				.Where(d => !d.IsDeleted)
				.ToListAsync();
			return View(departments);
		}

		// KULLANICI / KAYIT SİLME (Soft Delete - IsDeleted güncellemesi)
		[HttpPost]
		public async Task<IActionResult> DeleteUser(int id, string type)
		{
			if (type == "Student")
			{
				var user = await _context.Students.FindAsync(id);
				if (user != null) user.IsDeleted = true;
			}
			else if (type == "Academic")
			{
				var user = await _context.Academics.FindAsync(id);
				if (user != null) user.IsDeleted = true;
			}
			else if (type == "Admin")
			{
				var user = await _context.Admins.FindAsync(id);
				if (user != null) user.IsDeleted = true;
			}
			else if (type == "Company") // View'daki hatayı çözen kısım burası
			{
				var company = await _context.Companies.FindAsync(id);
				if (company != null) company.IsDeleted = true;
			}

			await _context.SaveChangesAsync();

			// Silme işleminden sonra geldiği listeye geri döner
			return RedirectToAction(type + "s");
		}

		// AYARLAR SAYFASI
		public async Task<IActionResult> Settings()
		{
			var ayarlar = await _context.SystemSettings.FirstOrDefaultAsync();
			if (ayarlar == null)
			{
				ayarlar = new SystemSettings
				{
					MaxLoading = 5120,
					AuthorizedFileExtensions = ".pdf,.jpg,.png",
					IsPeriod = true
				};
				_context.SystemSettings.Add(ayarlar);
				await _context.SaveChangesAsync();
			}
			return View(ayarlar);
		}

		// AYARLARI GÜNCELLEME
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateSettings(SystemSettings model)
		{
			if (ModelState.IsValid)
			{
				model.UpdatedDate = DateTime.UtcNow;
				_context.Update(model);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Settings));
			}
			return View("Settings", model);
		}

		// LOGOUT
		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Login", "Admin");
		}

		[HttpGet]
		public async Task<IActionResult> Profile()
		{
			var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (!int.TryParse(adminIdClaim, out int adminId))
				return RedirectToAction("Login", "Admin");

			var admin = await _context.Admins.FirstOrDefaultAsync(x => x.Id == adminId && !x.IsDeleted);

			if (admin == null)
				return RedirectToAction("Login", "Admin");

			var model = new AdminProfileViewModel
			{
				Id = admin.Id,
				FullName = admin.FullName,
				Email = admin.Email
			};

			return View(model);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditAcademic(int id, bool isSummerInternshipResponsible)
		{
			var academic = await _context.Academics
				.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

			if (academic == null) return NotFound();

			academic.IsSummerInternshipResponsible = isSummerInternshipResponsible;
			academic.UpdatedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = $"{academic.FullName} başarıyla güncellendi.";
			return RedirectToAction("Teachers"); 
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Profile(AdminProfileViewModel model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var admin = await _context.Admins.FirstOrDefaultAsync(x => x.Id == model.Id && !x.IsDeleted);

			if (admin == null)
				return NotFound();

			var emailExists = await _context.Admins.AnyAsync(x => x.Email == model.Email && x.Id != model.Id && !x.IsDeleted);
			if (emailExists)
			{
				ModelState.AddModelError("Email", "Bu e-posta adresi başka bir yönetici tarafından kullanılıyor.");
				return View(model);
			}

			admin.FullName = model.FullName;
			admin.Email = model.Email;

			if (!string.IsNullOrWhiteSpace(model.NewPassword))
			{
				admin.PasswordHash = model.NewPassword;
			}

			admin.UpdatedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			var claims = new List<Claim>
	{
		new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
		new Claim(ClaimTypes.Name, admin.FullName),
		new Claim(ClaimTypes.Email, admin.Email),
		new Claim(ClaimTypes.Role, "Admin")
	};

			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var principal = new ClaimsPrincipal(identity);

			await HttpContext.SignInAsync(
				CookieAuthenticationDefaults.AuthenticationScheme,
				principal,
				new AuthenticationProperties
				{
					IsPersistent = true
				});

			TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
			return RedirectToAction(nameof(Profile));
		}


	}
}