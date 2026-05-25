using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using WebTabanliStajTakipSistemi.Data;
using WebTabanliStajTakipSistemi.Enums;
using WebTabanliStajTakipSistemi.Models;

namespace WebTabanliStajTakipSistemi.Controllers
{
	[Authorize]
	public class InternshipDiaryController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IWebHostEnvironment _environment;

		public InternshipDiaryController(AppDbContext context, IWebHostEnvironment environment)
		{
			_context = context;
			_environment = environment;
		}

		[Authorize(Roles = "Student")]
		public async Task<IActionResult> Index()
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var diaries = await _context.InternshipDiaries
				.Include(d => d.Internship)
					.ThenInclude(i => i.Company)
				.Include(d => d.DiaryFiles.Where(f => !f.IsDeleted))
				.Where(d => d.StudentId == userId && !d.IsDeleted)
				.OrderByDescending(d => d.WorkDate)
				.ToListAsync();

			return View(diaries);
		}

		[Authorize(Roles = "Student")]
		[HttpGet]
		public async Task<IActionResult> Create()
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var activeInternship = await _context.Internships
				.Include(i => i.Company)
				.FirstOrDefaultAsync(i =>
					i.StudentId == userId &&
					!i.IsDeleted &&
					i.Status == InternshipStatus.Ongoing);

			if (activeInternship == null)
			{
				TempData["ErrorMessage"] = "Aktif staj kaydı bulunamadı. Günlük yazabilmek için devam eden bir stajınız olmalıdır.";
				return RedirectToAction(nameof(Index));
			}

			ViewBag.ActiveInternship = activeInternship;
			await LoadDiaryDatePickerDataAsync(activeInternship.Id, userId, activeInternship);

			var model = new InternshipDiary
			{
				InternshipId = activeInternship.Id,
				WorkDate = DateTime.Today
			};

			return View(model);
		}

		[Authorize(Roles = "Student")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(InternshipDiary diary)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var activeInternship = await _context.Internships
				.Include(i => i.Company)
				.FirstOrDefaultAsync(i =>
					i.StudentId == userId &&
					!i.IsDeleted &&
					i.Status == InternshipStatus.Ongoing);

			if (activeInternship == null)
			{
				TempData["ErrorMessage"] = "Aktif staj kaydı bulunamadı.";
				return RedirectToAction(nameof(Index));
			}

			// Öğrenci hangi hidden value gönderirse göndersin, biz aktif stajı zorunlu set ediyoruz
			diary.InternshipId = activeInternship.Id;

			var dateError = await ValidateDiaryWorkDateAsync(
					activeInternship.Id,
					userId,
					diary.WorkDate,
					null,
					activeInternship.StartDate,
					activeInternship.EndDate
				);

			if (dateError != null)
				ModelState.AddModelError(nameof(diary.WorkDate), dateError);

			ModelState.Remove("Internship");
			ModelState.Remove("Student");
			ModelState.Remove("ApprovedBy");
			ModelState.Remove("Comments");
			ModelState.Remove("DiaryFiles");

			if (!ModelState.IsValid)
			{
				ViewBag.ActiveInternship = activeInternship;
				await LoadDiaryDatePickerDataAsync(activeInternship.Id, userId, activeInternship);
				return View(diary);
			}

			diary.StudentId = userId;
			diary.Status = DiaryStatus.Draft;
			diary.CreatedDate = DateTime.UtcNow;

			_context.InternshipDiaries.Add(diary);
			await _context.SaveChangesAsync();

			await SyncDiaryFilesFromHtml(diary.Id, diary.Content);

			TempData["SuccessMessage"] = "Staj defteri kaydı oluşturuldu.";
			return RedirectToAction(nameof(Index));
		}

		[Authorize(Roles = "Student")]
		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var diary = await _context.InternshipDiaries
				.Include(d => d.DiaryFiles.Where(f => !f.IsDeleted))
				.Include(d => d.Internship)
					.ThenInclude(i => i.Company)
				.FirstOrDefaultAsync(d => d.Id == id && d.StudentId == userId && !d.IsDeleted);

			if (diary == null)
				return NotFound();

			if (diary.Status == DiaryStatus.Submitted || diary.Status == DiaryStatus.Approved)
			{
				TempData["ErrorMessage"] = "Gönderilmiş veya onaylanmış kayıt düzenlenemez.";
				return RedirectToAction(nameof(Index));
			}

			ViewBag.ActiveInternship = diary.Internship;
			return View(diary);
		}

		[Authorize(Roles = "Student")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(InternshipDiary diary)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var existingDiary = await _context.InternshipDiaries
				.Include(d => d.DiaryFiles.Where(f => !f.IsDeleted))
				.Include(d => d.Internship)
					.ThenInclude(i => i.Company)
				.FirstOrDefaultAsync(d => d.Id == diary.Id && d.StudentId == userId && !d.IsDeleted);

			if (existingDiary == null)
				return NotFound();

			if (existingDiary.Status == DiaryStatus.Submitted || existingDiary.Status == DiaryStatus.Approved)
			{
				TempData["ErrorMessage"] = "Gönderilmiş veya onaylanmış kayıt düzenlenemez.";
				return RedirectToAction(nameof(Index));
			}

			ModelState.Remove("Internship");
			ModelState.Remove("Student");
			ModelState.Remove("ApprovedBy");
			ModelState.Remove("Comments");
			ModelState.Remove("DiaryFiles");

			// Edit ekranında tarih değiştirilmeyecek.
			// WorkDate post edilse bile dikkate almıyoruz.
			ModelState.Remove(nameof(diary.WorkDate));

			if (!ModelState.IsValid)
			{
				ViewBag.ActiveInternship = existingDiary.Internship;

				existingDiary.Title = diary.Title;
				existingDiary.Content = diary.Content;

				return View(existingDiary);
			}

			// Güvenlik: InternshipId ve WorkDate değiştirtmiyoruz.
			existingDiary.Title = diary.Title;
			existingDiary.Content = diary.Content;
			existingDiary.UpdatedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			await SyncDiaryFilesFromHtml(existingDiary.Id, existingDiary.Content);

			TempData["SuccessMessage"] = "Staj defteri kaydı güncellendi.";
			return RedirectToAction(nameof(Index));
		}

		[Authorize(Roles = "Student,Academic")]
		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var query = _context.InternshipDiaries
				.Include(d => d.Internship)
					.ThenInclude(i => i.Company)
				.Include(d => d.Student)
				.Include(d => d.DiaryFiles.Where(f => !f.IsDeleted))
				.Include(d => d.Comments.Where(c => !c.IsDeleted))
					.ThenInclude(c => c.Academic)
				.Where(d => d.Id == id && !d.IsDeleted);

			if (User.IsInRole("Student"))
			{
				query = query.Where(d => d.StudentId == userId);
			}
			else if (User.IsInRole("Academic"))
			{
				query = query.Where(d => d.Internship.AcademicId == userId);
			}

			var diary = await query.FirstOrDefaultAsync();

			if (diary == null)
				return NotFound();

			return View(diary);
		}

		[HttpPost]
		[Authorize(Roles = "Academic")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddComment(int diaryId, string description)
		{
			var academicId = int.Parse(User.FindFirst("UserId")!.Value);

			if (string.IsNullOrWhiteSpace(description))
			{
				TempData["ErrorMessage"] = "Yorum boş olamaz.";
				return RedirectToAction(nameof(Details), new { id = diaryId });
			}

			var diary = await _context.InternshipDiaries
				.Include(d => d.Internship)
				.FirstOrDefaultAsync(d => d.Id == diaryId && !d.IsDeleted);

			if (diary == null || diary.Internship.AcademicId != academicId)
				return NotFound();

			var comment = new Comment
			{
				AcademicId = academicId,
				InternshipDiaryId = diary.Id,
				Description = description.Trim(),
				CreatedDate = DateTime.UtcNow
			};

			_context.Comments.Add(comment);

			diary.UpdatedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Yorum eklendi.";
			return RedirectToAction(nameof(Details), new { id = diaryId });
		}

		[Authorize(Roles = "Student")]
		[HttpGet]
		public async Task<IActionResult> Print(int id)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var diary = await _context.InternshipDiaries
				.Include(d => d.Student)
				.Include(d => d.Internship)
					.ThenInclude(i => i.Company)
				.Include(d => d.DiaryFiles.Where(f => !f.IsDeleted))
				.FirstOrDefaultAsync(d => d.Id == id && d.StudentId == userId && !d.IsDeleted);

			if (diary == null)
				return NotFound();

			return View(diary);
		}

		[Authorize(Roles = "Student")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Submit(int id)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var diary = await _context.InternshipDiaries
				.FirstOrDefaultAsync(d => d.Id == id && d.StudentId == userId && !d.IsDeleted);

			if (diary == null)
				return NotFound();

			if (diary.Status == DiaryStatus.Approved)
			{
				TempData["ErrorMessage"] = "Onaylı kayıt tekrar gönderilemez.";
				return RedirectToAction(nameof(Index));
			}

			diary.Status = DiaryStatus.Submitted;
			diary.SubmittedDate = DateTime.UtcNow;
			diary.UpdatedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Kayıt incelemeye gönderildi.";
			return RedirectToAction(nameof(Index));
		}

		[Authorize(Roles = "Student")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var diary = await _context.InternshipDiaries
				.FirstOrDefaultAsync(d => d.Id == id && d.StudentId == userId && !d.IsDeleted);

			if (diary == null)
				return NotFound();

			if (diary.Status == DiaryStatus.Submitted || diary.Status == DiaryStatus.Approved)
			{
				TempData["ErrorMessage"] = "Gönderilmiş veya onaylanmış kayıt silinemez.";
				return RedirectToAction(nameof(Index));
			}

			diary.IsDeleted = true;
			diary.UpdatedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Kayıt silindi.";
			return RedirectToAction(nameof(Index));
		}

		[Authorize(Roles = "Student")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteImage(int imageId)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var image = await _context.DiaryFiles
				.Include(x => x.InternshipDiary)
				.FirstOrDefaultAsync(x => x.Id == imageId && !x.IsDeleted);

			if (image == null || image.InternshipDiary.StudentId != userId)
				return NotFound();

			if (image.InternshipDiary.Status == DiaryStatus.Submitted || image.InternshipDiary.Status == DiaryStatus.Approved)
			{
				TempData["ErrorMessage"] = "Gönderilmiş veya onaylanmış kaydın görselleri silinemez.";
				return RedirectToAction(nameof(Edit), new { id = image.InternshipDiaryId });
			}

			image.IsDeleted = true;
			image.UpdatedDate = DateTime.UtcNow;
			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Görsel silindi.";
			return RedirectToAction(nameof(Edit), new { id = image.InternshipDiaryId });
		}

		[Authorize(Roles = "Student")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UploadEditorImage(IFormFile file)
		{
			var userIdClaim = User.FindFirst("UserId")?.Value;
			if (!int.TryParse(userIdClaim, out int studentId))
				return BadRequest(new { error = "Oturum bulunamadı." });

			if (file == null || file.Length == 0)
				return BadRequest(new { error = "Dosya seçilmedi." });

			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

			if (!allowedExtensions.Contains(extension))
				return BadRequest(new { error = "Sadece görsel dosyaları yüklenebilir." });

			var folder = Path.Combine(_environment.WebRootPath, "uploads", "diaries", "temp", studentId.ToString());

			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);

			var uniqueFileName = $"{Guid.NewGuid()}{extension}";
			var fullPath = Path.Combine(folder, uniqueFileName);

			using (var stream = new FileStream(fullPath, FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}

			var publicPath = $"/uploads/diaries/temp/{studentId}/{uniqueFileName}";
			return Json(new { url = publicPath });
		}

		private async Task SyncDiaryFilesFromHtml(int diaryId, string? htmlContent)
		{
			if (string.IsNullOrWhiteSpace(htmlContent))
				return;

			var existingFiles = await _context.DiaryFiles
				.Where(x => x.InternshipDiaryId == diaryId && !x.IsDeleted)
				.ToListAsync();

			var matches = Regex.Matches(htmlContent, "src=[\"'](?<src>.*?)[\"']", RegexOptions.IgnoreCase);
			var imagePaths = matches
				.Select(m => m.Groups["src"].Value)
				.Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith("/uploads/diaries/"))
				.Distinct()
				.ToList();

			foreach (var path in imagePaths)
			{
				if (existingFiles.Any(f => f.FilePath == path))
					continue;

				var fileName = Path.GetFileName(path);
				var extension = Path.GetExtension(path).ToLowerInvariant();
				var physicalPath = Path.Combine(
					_environment.WebRootPath,
					path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
				);

				long fileSize = 0;
				if (System.IO.File.Exists(physicalPath))
				{
					var info = new FileInfo(physicalPath);
					fileSize = info.Length;
				}

				_context.DiaryFiles.Add(new DiaryFile
				{
					InternshipDiaryId = diaryId,
					FileName = fileName,
					FilePath = path,
					ContentType = GetContentType(extension),
					FileSize = fileSize
				});
			}

			foreach (var existing in existingFiles)
			{
				if (!imagePaths.Contains(existing.FilePath))
				{
					existing.IsDeleted = true;
					existing.UpdatedDate = DateTime.UtcNow;
				}
			}

			await _context.SaveChangesAsync();
		}

		private string GetContentType(string extension)
		{
			return extension switch
			{
				".jpg" => "image/jpeg",
				".jpeg" => "image/jpeg",
				".png" => "image/png",
				".webp" => "image/webp",
				_ => "application/octet-stream"
			};
		}

		[HttpPost]
		[Authorize(Roles = "Academic")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Approve(int id)
		{
			var academicId = int.Parse(User.FindFirst("UserId")!.Value);

			var diary = await _context.InternshipDiaries
				.Include(d => d.Internship)
				.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

			if (diary == null || diary.Internship.AcademicId != academicId)
				return NotFound();

			if (diary.Status != DiaryStatus.Submitted && diary.Status != DiaryStatus.RevisionRequested)
			{
				TempData["ErrorMessage"] = "Sadece gönderilmiş veya revizyon sonrası gönderilmiş kayıtlar onaylanabilir.";
				return RedirectToAction(nameof(Details), new { id });
			}

			diary.Status = DiaryStatus.Approved;
			diary.ApprovedDate = DateTime.UtcNow;
			diary.ApprovedById = academicId;
			diary.UpdatedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Günlük kaydı onaylandı.";
			return RedirectToAction("DiaryReviews", "Academic");
		}

		[HttpPost]
		[Authorize(Roles = "Academic")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RequestRevision(int id, string revisionComment)
		{
			var academicId = int.Parse(User.FindFirst("UserId")!.Value);

			var diary = await _context.InternshipDiaries
				.Include(d => d.Internship)
				.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

			if (diary == null || diary.Internship.AcademicId != academicId)
				return NotFound();

			if (diary.Status == DiaryStatus.Approved)
			{
				TempData["ErrorMessage"] = "Onaylanmış günlük için revizyon istenemez.";
				return RedirectToAction(nameof(Details), new { id });
			}

			if (string.IsNullOrWhiteSpace(revisionComment))
			{
				TempData["ErrorMessage"] = "Revizyon istemek için açıklama yazmalısınız.";
				return RedirectToAction(nameof(Details), new { id });
			}

			_context.Comments.Add(new Comment
			{
				AcademicId = academicId,
				InternshipDiaryId = diary.Id,
				Description = revisionComment.Trim(),
				CreatedDate = DateTime.UtcNow
			});

			diary.Status = DiaryStatus.RevisionRequested;
			diary.RevisionRequestedDate = DateTime.UtcNow;
			diary.UpdatedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Günlük kaydı revizyona gönderildi.";
			return RedirectToAction("DiaryReviews", "Academic");
		}


		private async Task<List<string>> GetUsedDiaryDateStringsAsync(
			int internshipId,
			int studentId,
			int? excludeDiaryId = null)
		{
			var usedDates = await _context.InternshipDiaries
				.Where(d =>
					d.InternshipId == internshipId &&
					d.StudentId == studentId &&
					!d.IsDeleted &&
					(!excludeDiaryId.HasValue || d.Id != excludeDiaryId.Value))
				.Select(d => d.WorkDate.Date)
				.ToListAsync();

			return usedDates
				.Distinct()
				.Select(d => d.ToString("yyyy-MM-dd"))
				.ToList();
		}

		private async Task<string?> ValidateDiaryWorkDateAsync(
			int internshipId,
			int studentId,
			DateTime workDate,
			int? excludeDiaryId,
			DateTime internshipStartDate,
			DateTime internshipEndDate)
		{
			var selectedDate = workDate.Date;
			var today = DateTime.Today;

			if (selectedDate < internshipStartDate.Date || selectedDate > internshipEndDate.Date)
				return "Çalışma tarihi staj tarih aralığı içinde olmalıdır.";

			if (selectedDate > today)
				return "Gelecek tarih için günlük kaydı oluşturulamaz.";

			var nextDate = selectedDate.AddDays(1);

			var exists = await _context.InternshipDiaries.AnyAsync(d =>
				d.InternshipId == internshipId &&
				d.StudentId == studentId &&
				!d.IsDeleted &&
				d.WorkDate >= selectedDate &&
				d.WorkDate < nextDate &&
				(!excludeDiaryId.HasValue || d.Id != excludeDiaryId.Value));

			if (exists)
				return "Bu çalışma tarihi için daha önce günlük kaydı oluşturulmuş. Lütfen farklı bir tarih seçin.";

			return null;
		}

		private async Task LoadDiaryDatePickerDataAsync(
			int internshipId,
			int studentId,
			Internship internship,
			int? excludeDiaryId = null)
		{
			ViewBag.DisabledDiaryDates = await GetUsedDiaryDateStringsAsync(
				internshipId,
				studentId,
				excludeDiaryId
			);

			ViewBag.MinWorkDate = internship.StartDate.Date.ToString("yyyy-MM-dd");

			var maxDate = internship.EndDate.Date < DateTime.Today
				? internship.EndDate.Date
				: DateTime.Today;

			ViewBag.MaxWorkDate = maxDate.ToString("yyyy-MM-dd");
		}
	}
}