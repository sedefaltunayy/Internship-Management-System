using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebTabanliStajTakipSistemi.Data;
using WebTabanliStajTakipSistemi.Enums;
using WebTabanliStajTakipSistemi.Models;

namespace WebTabanliStajTakipSistemi.Controllers
{
	public class AcademicController : Controller
	{
		private readonly AppDbContext _context;

		public AcademicController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(string academicNumber, string password)
		{
			var academic = await _context.Academics
				.FirstOrDefaultAsync(a =>
					a.AcademicNumber == academicNumber &&
					a.Password == password &&
					!a.IsDeleted);

			if (academic == null)
			{
				ViewBag.Hata = "Sicil numarası veya şifre hatalı!";
				return View();
			}

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.Name, academic.FullName),
				new Claim(ClaimTypes.Email, academic.Email ?? ""),
				new Claim(ClaimTypes.Role, "Academic"),
				new Claim("UserId", academic.Id.ToString())
			};

			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var principal = new ClaimsPrincipal(identity);

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

			return RedirectToAction(nameof(Index));
		}

		[Authorize(Roles = "Academic")]
		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction(nameof(Login));
		}

		[HttpGet]
		public async Task<IActionResult> Register()
		{
			await PopulateDropdowns();
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(Academic model)
		{
			ModelState.Remove("Department");
			ModelState.Remove("University");
			ModelState.Remove("Internships");

			if (!ModelState.IsValid)
			{
				await PopulateDropdowns();
				return View(model);
			}

			var emailExists = await _context.Academics
				.AnyAsync(a => a.Email == model.Email && !a.IsDeleted);

			if (emailExists)
			{
				ModelState.AddModelError("Email", "Bu e-posta adresi zaten kayıtlı.");
				await PopulateDropdowns();
				return View(model);
			}

			var sicilExists = await _context.Academics
				.AnyAsync(a => a.AcademicNumber == model.AcademicNumber && !a.IsDeleted);

			if (sicilExists)
			{
				ModelState.AddModelError("AcademicNumber", "Bu sicil numarası zaten kayıtlı.");
				await PopulateDropdowns();
				return View(model);
			}

			_context.Academics.Add(model);
			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Akademisyen kaydı başarıyla oluşturuldu.";
			return RedirectToAction(nameof(Login));
		}


		[Authorize(Roles = "Academic")]
		public async Task<IActionResult> Index()
		{
			var academicId = int.Parse(User.FindFirst("UserId")!.Value);

			// Ön onay bekleyenler
			ViewBag.PendingApplicationsCount = await _context.Internships
				.CountAsync(i => i.AcademicId == academicId &&
								 !i.IsDeleted &&
								 i.Status == InternshipStatus.Pending);

			// Sözleşme yüklenmiş, son onay bekleyenler ✅
			ViewBag.PendingContractsCount = await _context.Internships
				.CountAsync(i => i.AcademicId == academicId &&
								 !i.IsDeleted &&
								 i.Status == InternshipStatus.ContractUploaded);

			ViewBag.ActiveInternshipsCount = await _context.Internships
				.CountAsync(i => i.AcademicId == academicId &&
								 !i.IsDeleted &&
								 i.Status == InternshipStatus.Ongoing);

			ViewBag.CompletedInternshipsCount = await _context.Internships
				.CountAsync(i => i.AcademicId == academicId &&
								 !i.IsDeleted &&
								 i.Status == InternshipStatus.Completed);

			return View();
		}

		[Authorize(Roles = "Academic")]
		[HttpGet]
		public async Task<IActionResult> Profile()
		{
			var academicId = int.Parse(User.FindFirst("UserId")!.Value);

			var academic = await _context.Academics
				.Include(a => a.Department)
				.Include(a => a.University)
				.FirstOrDefaultAsync(a => a.Id == academicId && !a.IsDeleted);

			if (academic == null)
				return RedirectToAction(nameof(Login));

			return View(academic);
		}

		[Authorize(Roles = "Academic")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Profile(Academic model)
		{
			var academicId = int.Parse(User.FindFirst("UserId")!.Value);

			var academic = await _context.Academics
				.FirstOrDefaultAsync(a => a.Id == academicId && !a.IsDeleted);

			if (academic == null)
				return RedirectToAction(nameof(Login));

			var emailExists = await _context.Academics
				.AnyAsync(a => a.Email == model.Email && a.Id != academicId && !a.IsDeleted);

			if (emailExists)
			{
				ModelState.AddModelError("Email", "Bu e-posta adresi başka bir akademisyen tarafından kullanılıyor.");
				return View(model);
			}

			academic.FullName = model.FullName;
			academic.Email = model.Email;
			academic.Title = model.Title;

			if (!string.IsNullOrWhiteSpace(model.Password))
			{
				academic.Password = model.Password;
			}

			academic.UpdatedDate = DateTime.UtcNow;
			await _context.SaveChangesAsync();

			// claim güncelle
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.Name, academic.FullName),
				new Claim(ClaimTypes.Email, academic.Email ?? ""),
				new Claim(ClaimTypes.Role, "Academic"),
				new Claim("UserId", academic.Id.ToString())
			};

			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var principal = new ClaimsPrincipal(identity);

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

			TempData["SuccessMessage"] = "Profil bilgileriniz güncellendi.";
			return RedirectToAction(nameof(Profile));
		}

		// ---------------- HELPERS ----------------

		private async Task PopulateDropdowns()
		{
			ViewBag.Universities = new SelectList(
				await _context.Universities
					.Where(u => !u.IsDeleted)
					.OrderBy(u => u.Name)
					.ToListAsync(),
				"Id",
				"Name"
			);

			ViewBag.Departments = new SelectList(
				await _context.Departments
					.Where(d => !d.IsDeleted)
					.OrderBy(d => d.Name)
					.ToListAsync(),
				"Id",
				"Name"
			);
		}

		[Authorize(Roles = "Academic")]
		public async Task<IActionResult> ActiveInternships()
		{
			var academicId = int.Parse(User.FindFirst("UserId")!.Value);

			var internships = await _context.Internships
				.Include(i => i.Student)
				.Include(i => i.Company)
				.Where(i => i.AcademicId == academicId && !i.IsDeleted && i.Status == InternshipStatus.Ongoing)
				.OrderByDescending(i => i.StartDate)
				.ToListAsync();

			return View(internships);
		}

		[Authorize(Roles = "Academic")]
		public async Task<IActionResult> PendingContracts()
		{
			var academicId = int.Parse(User.FindFirst("UserId")!.Value);

			var list = await _context.Internships
				.Include(i => i.Student)
				.Include(i => i.Company)
				.Include(i => i.InternshipFiles.Where(f => !f.IsDeleted))
				.Where(i => i.AcademicId == academicId &&
							!i.IsDeleted &&
							i.Status == InternshipStatus.ContractUploaded)
				.OrderByDescending(i => i.ContractUploadedDate)
				.ToListAsync();

			return View(list);
		}

		[Authorize(Roles = "Academic")]
		[HttpGet]
		public async Task<IActionResult> DiaryReviews(int? studentId, DiaryStatus? status)
		{
			var academicId = int.Parse(User.FindFirst("UserId")!.Value);

			var baseQuery = _context.InternshipDiaries
				.Include(d => d.Student)
				.Include(d => d.Internship)
					.ThenInclude(i => i.Company)
				.Include(d => d.Comments.Where(c => !c.IsDeleted))
				.Where(d =>
					!d.IsDeleted &&
					d.Internship.AcademicId == academicId &&
					(
						d.Status == DiaryStatus.Submitted ||
						d.Status == DiaryStatus.RevisionRequested ||
						d.Status == DiaryStatus.Approved
					));

			var studentOptions = await baseQuery
				.Select(d => new
				{
					Id = d.StudentId,
					Name = d.Student.FullName
				})
				.Distinct()
				.OrderBy(x => x.Name)
				.ToListAsync();

			if (studentId.HasValue && studentId.Value > 0)
			{
				baseQuery = baseQuery.Where(d => d.StudentId == studentId.Value);
			}

			if (status.HasValue)
			{
				baseQuery = baseQuery.Where(d => d.Status == status.Value);
			}

			var diaries = await baseQuery
				.OrderByDescending(d => d.WorkDate)
				.ToListAsync();

			ViewBag.StudentOptions = studentOptions;
			ViewBag.SelectedStudentId = studentId;
			ViewBag.SelectedStatus = status;

			return View("DiaryReviews", diaries);
		}
	}
}