using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using WebTabanliStajTakipSistemi.Data;
using WebTabanliStajTakipSistemi.Models;
using WebTabanliStajTakipSistemi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using WebTabanliStajTakipSistemi.Enums;
namespace WebTabanliStajTakipSistemi.Controllers
{
	public class StudentController : Controller
	{
		private readonly AppDbContext _context;

		public StudentController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Login() => View();

		[HttpPost]
		[ValidateAntiForgeryToken]
		[AllowAnonymous]
		public async Task<IActionResult> Login(string number, string password)
		{
			var student = await _context.Students
				.FirstOrDefaultAsync(u => u.StudentNumber == number && u.Password == password && !u.IsDeleted);

			if (student != null)
			{
				var claims = new List<Claim>
				{
					new Claim(ClaimTypes.Name, student.FullName),
					new Claim(ClaimTypes.Role, "Student"),
					new Claim("UserId", student.Id.ToString())
				};

				var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
				var principal = new ClaimsPrincipal(identity);

				await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

				return RedirectToAction(nameof(Index));
			}

			ViewBag.Hata = "Öğrenci numarası veya şifre hatalı!";
			return View();
		}

		[Authorize(Roles = "Student")]
		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction(nameof(Login));
		}

		
		[Authorize(Roles = "Student")]
		public async Task<IActionResult> Index()
		{
			var userIdClaim = User.FindFirst("UserId")?.Value;

			if (!int.TryParse(userIdClaim, out int studentId))
				return RedirectToAction(nameof(Login));

			await ActivateEligibleInternships(studentId);

			var student = await _context.Students
				.Include(s => s.Department)
				.Include(s => s.University)
				.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);

			if (student == null)
				return RedirectToAction(nameof(Login));

			var latestInternship = await _context.Internships
				.Include(i => i.Company)
				.Include(i => i.InternshipFiles.Where(f => !f.IsDeleted))
				.Include(i => i.InternshipDiaries.Where(d => !d.IsDeleted))
				.Where(i => i.StudentId == studentId && !i.IsDeleted)
				.OrderByDescending(i => i.CreatedDate)
				.FirstOrDefaultAsync();

			ViewBag.StudentNumber = student.StudentNumber;
			ViewBag.DepartmentName = student.Department?.Name ?? "Belirtilmemiş";
			ViewBag.UniversityName = student.University?.Name ?? "Belirtilmemiş";

			if (latestInternship == null)
			{
				ViewBag.BasvuruDurumu = "Henüz Başvuru Yok";
				ViewBag.StajTuru = "Belirtilmemiş";
				ViewBag.BelgeDurumu = "Belge Yok";
				ViewBag.DefterDurumu = "Henüz Başlamadı";
				ViewBag.CompanyName = "-";
				ViewBag.Position = "-";
				ViewBag.StartDate = null;
				ViewBag.EndDate = null;
			}
			else
			{
				ViewBag.BasvuruDurumu = GetInternshipStatusText(latestInternship.Status);

				ViewBag.StajTuru = GetInternshipTypeText(latestInternship.Types);

				ViewBag.BelgeDurumu =
					latestInternship.InternshipFiles != null && latestInternship.InternshipFiles.Any()
					? $"{latestInternship.InternshipFiles.Count} Belge Yüklendi"
					: "Belge Yok";

				if (latestInternship.InternshipDiaries != null && latestInternship.InternshipDiaries.Any())
				{
					var latestDiary = latestInternship.InternshipDiaries
						.OrderByDescending(d => d.CreatedDate)
						.FirstOrDefault();

					ViewBag.DefterDurumu = latestDiary == null
						? "Henüz Oluşturulmadı"
						: latestDiary.Status switch
						{
							DiaryStatus.Draft => "Taslak",
							DiaryStatus.Submitted => "Gönderildi",
							DiaryStatus.RevisionRequested => "Revizyon İstendi",
							DiaryStatus.Approved => "Onaylandı",
							_ => "Henüz Oluşturulmadı"
						};
				}
				else
				{
					ViewBag.DefterDurumu = "Henüz Oluşturulmadı";
				}

				ViewBag.CompanyName = latestInternship.Company?.Name ?? "-";
				ViewBag.Position = latestInternship.Position ?? "-";
				ViewBag.StartDate = latestInternship.StartDate;
				ViewBag.EndDate = latestInternship.EndDate;
			}

			ViewData["Title"] = "Anasayfa";
			return View();
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Register()
		{
			await PopulateDropdowns();
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[AllowAnonymous]
		public async Task<IActionResult> Register(Student student)
		{
			ModelState.Remove("Department");
			ModelState.Remove("University");
			ModelState.Remove("Internships");
			if (!ModelState.IsValid)
			{
				foreach (var kvp in ModelState)
				{
					var key = kvp.Key;
					var errors = kvp.Value.Errors;

					foreach (var error in errors)
					{
						Console.WriteLine($"ModelState Hatası - Alan: {key} | Hata: {error.ErrorMessage}");
					}
				}
				await PopulateDropdowns();
				return View(student);

			}

			try
			{
				_context.Students.Add(student);
				await _context.SaveChangesAsync();
				Console.WriteLine("Öğrenci başarıyla kaydedildi.");
				return RedirectToAction(nameof(Login));
			}
			catch (Exception ex)
			{
				Console.WriteLine("KAYIT HATASI: " + ex.Message);
				Console.WriteLine("INNER: " + ex.InnerException?.Message);

				ModelState.AddModelError("", "Kayıt sırasında bir hata oluştu: " + ex.InnerException?.Message ?? ex.Message);

				await PopulateDropdowns();
				return View(student);
			}
		}

		private async Task PopulateDropdowns()
		{
			ViewBag.UniversityId = new SelectList(
				await _context.Universities.Where(u => !u.IsDeleted).ToListAsync(),
				"Id",
				"Name"
			);

			ViewBag.DepartmentId = new SelectList(
				await _context.Departments.Where(d => !d.IsDeleted).ToListAsync(),
				"Id",
				"Name"
			);
		}


		//Linkler hata vermesin diye eklendi 
		[Authorize(Roles = "Student")]
		public IActionResult Applications()
		{
			return RedirectToAction("Index", "Internship");
		}

		[Authorize(Roles = "Student")]
		public async Task<IActionResult> Documents()
		{
			var userIdClaim = User.FindFirst("UserId")?.Value;
			if (!int.TryParse(userIdClaim, out int studentId))
				return RedirectToAction(nameof(Login));

			var internship = await _context.Internships
				.Where(i => i.StudentId == studentId && !i.IsDeleted)
				.OrderByDescending(i => i.CreatedDate)
				.FirstOrDefaultAsync();

			if (internship == null)
			{
				TempData["ErrorMessage"] = "Henüz staj başvurunuz bulunmuyor.";
				return RedirectToAction(nameof(Index));
			}

			return RedirectToAction("Documents", "Internship", new { id = internship.Id });
		}


		[HttpGet]
		[Authorize(Roles = "Student")]
		public async Task<IActionResult> Profile()
		{
			var userIdClaim = User.FindFirst("UserId")?.Value;

			if (!int.TryParse(userIdClaim, out int studentId))
				return RedirectToAction(nameof(Login));

			var student = await _context.Students
				.Include(s => s.Department)
				.Include(s => s.University)
				.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);

			if (student == null)
				return RedirectToAction(nameof(Login));

			var model = new StudentProfileViewModel
			{
				Id = student.Id,
				FullName = student.FullName,
				Email = student.Email,
				StudentNumber = student.StudentNumber,
				UniversityName = student.University?.Name ?? "Belirtilmemiş",
				DepartmentName = student.Department?.Name ?? "Belirtilmemiş"
			};

			ViewData["Title"] = "Profilim";
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Student")]
		public async Task<IActionResult> Profile(StudentProfileViewModel model)
		{
			if (!ModelState.IsValid)
			{
				ViewData["Title"] = "Profilim";
				return View(model);
			}

			var userIdClaim = User.FindFirst("UserId")?.Value;

			if (!int.TryParse(userIdClaim, out int studentId))
				return RedirectToAction(nameof(Login));

			var student = await _context.Students
				.Include(s => s.Department)
				.Include(s => s.University)
				.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);

			if (student == null)
				return RedirectToAction(nameof(Login));

			var emailExists = await _context.Students
				.AnyAsync(x => x.Email == model.Email && x.Id != student.Id && !x.IsDeleted);

			if (emailExists)
			{
				model.StudentNumber = student.StudentNumber;
				model.UniversityName = student.University?.Name ?? "Belirtilmemiş";
				model.DepartmentName = student.Department?.Name ?? "Belirtilmemiş";

				ModelState.AddModelError("Email", "Bu e-posta adresi başka bir öğrenci tarafından kullanılıyor.");
				ViewData["Title"] = "Profilim";
				return View(model);
			}

			student.FullName = model.FullName;
			student.Email = model.Email;

			if (!string.IsNullOrWhiteSpace(model.NewPassword))
			{
				student.Password = model.NewPassword;
			}

			student.UpdatedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			var claims = new List<Claim>
	{
		new Claim(ClaimTypes.Name, student.FullName),
		new Claim(ClaimTypes.Role, "Student"),
		new Claim("UserId", student.Id.ToString())
	};

			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var principal = new ClaimsPrincipal(identity);

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

			TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";

			return RedirectToAction(nameof(Profile));
		}

		private string GetInternshipStatusText(InternshipStatus status)
		{
			return status switch
			{
				InternshipStatus.Pending => "Ön Onay Bekleniyor",
				InternshipStatus.ApplicationApproved => "Sözleşme Bekleniyor",
				InternshipStatus.ContractUploaded => "Son Onay Bekleniyor",
				InternshipStatus.Approved => "Staj Onaylandı",
				InternshipStatus.Ongoing => "Devam Ediyor",
				InternshipStatus.Completed => "Tamamlandı",
				InternshipStatus.Rejected => "Reddedildi",
				_ => "Belirsiz"
			};
		}

		private string GetInternshipTypeText(InternshipTypes type)
		{
			return type switch
			{
				InternshipTypes.Summer => "Yaz Stajı",
				InternshipTypes.LongTime => "Uzun Dönem",
				InternshipTypes.Optional => "Gönüllü",
				_ => "Belirtilmemiş"
			};
		}

		private string GetDiaryStatusText(DiaryStatus status)
		{
			return status switch
			{
				DiaryStatus.Draft => "Taslak",
				DiaryStatus.Submitted => "Gönderildi",
				DiaryStatus.RevisionRequested => "Revizyon İstendi",
				DiaryStatus.Approved => "Onaylandı",
				_ => "Henüz Oluşturulmadı"
			};
		}

		private async Task ActivateEligibleInternships(int studentId)
		{
			var internships = await _context.Internships
				.Where(i => i.StudentId == studentId &&
							!i.IsDeleted &&
							i.IsApplicationApproved &&
							i.IsContractApproved &&
							i.Status == InternshipStatus.Approved &&
							i.StartDate.Date <= DateTime.Today)
				.ToListAsync();

			foreach (var internship in internships)
			{
				internship.Status = InternshipStatus.Ongoing;
				internship.UpdatedDate = DateTime.UtcNow;
			}

			if (internships.Any())
				await _context.SaveChangesAsync();
		}
	}
}