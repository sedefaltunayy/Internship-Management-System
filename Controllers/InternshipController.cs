using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebTabanliStajTakipSistemi.Data;
using WebTabanliStajTakipSistemi.Enums;
using WebTabanliStajTakipSistemi.Models;
using WebTabanliStajTakipSistemi.ViewModels;

namespace WebTabanliStajTakipSistemi.Controllers
{
	[Authorize]
	public class InternshipController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IWebHostEnvironment _environment;

		public InternshipController(AppDbContext context, IWebHostEnvironment environment)
		{
			_context = context;
			_environment = environment;
		}

		[Authorize(Roles = "Student")]
		public async Task<IActionResult> Index()
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			await ActivateEligibleInternships(userId);

			var internships = await _context.Internships
				.Include(i => i.Company)
				.Include(i => i.Academic)
				.Include(i => i.InternshipFiles.Where(f => !f.IsDeleted))
				.Where(i => i.StudentId == userId && !i.IsDeleted)
				.OrderByDescending(i => i.CreatedDate)
				.ToListAsync();

			return View(internships);
		}

		[Authorize(Roles = "Student")]
		[HttpGet]
		public async Task<IActionResult> Create()
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var student = await _context.Students
						.Include(s => s.Department)
						.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

			if (student == null) return NotFound();

			await LoadCreateDropdowns(student.DepartmentId, null, null);

			ViewBag.InternshipTypes = new SelectList(
				Enum.GetValues(typeof(InternshipTypes))
					.Cast<InternshipTypes>()
					.Where(x => x != InternshipTypes.Unknown)
					.Select(x => new
					{
						Id = (int)x,
						Name = x switch
						{
							InternshipTypes.Summer => "Yaz Stajı",
							InternshipTypes.LongTime => "Uzun Dönem",
							InternshipTypes.Optional => "Gönüllü",
							_ => "Bilinmiyor"
						}
					}),
				"Id", "Name"
			);

			// Ülkeler — Türkiye en üstte
			ViewBag.Countries = await _context.Countries
				.Where(c => !c.IsDeleted)
				.OrderByDescending(c => c.Code == "TR")
				.ThenBy(c => c.Name)
				.Select(c => new { c.Id, c.Name })
				.ToListAsync();

			return View(new InternshipCreateViewModel());
		}

		[Authorize(Roles = "Student")]
		[HttpGet]
		public async Task<IActionResult> GetCompanyInfo(int id)
		{
			var company = await _context.Companies
				.Include(c => c.City)
				.Include(c => c.Country)
				.Where(c => c.Id == id && !c.IsDeleted)
				.Select(c => new
				{
					c.Id,
					c.Name,
					c.Address,
					c.Phone,
					c.Email,
					c.CompanyRepresentative,
					c.WebAddress,
					c.TaxNumber,
					c.CountryId,
					c.CityId,
					city = c.City != null ? c.City.Name : "",
					country = c.Country != null ? c.Country.Name : ""
				})
				.FirstOrDefaultAsync();

			if (company == null)
				return NotFound();

			return Json(company);
		}
		[HttpPost]
		[Authorize(Roles = "Student")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(InternshipCreateViewModel model)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var student = await _context.Students
				.Include(u => u.Department)
				.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

			if (student == null)
				return NotFound();

			if (!model.StartDate.HasValue)
				ModelState.AddModelError(nameof(model.StartDate), "Başlangıç tarihi zorunludur.");

			if (!model.EndDate.HasValue)
				ModelState.AddModelError(nameof(model.EndDate), "Bitiş tarihi zorunludur.");

			if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate.Value < model.StartDate.Value)
				ModelState.AddModelError(nameof(model.EndDate), "Bitiş tarihi başlangıç tarihinden önce olamaz.");

			if (model.Types == InternshipTypes.Unknown)
				ModelState.AddModelError(nameof(model.Types), "Staj türü seçiniz.");

			if (string.IsNullOrWhiteSpace(model.Position))
				ModelState.AddModelError(nameof(model.Position), "Pozisyon zorunludur.");

			if (!model.SelectedCompanyId.HasValue && string.IsNullOrWhiteSpace(model.CompanyName))
				ModelState.AddModelError(nameof(model.CompanyName), "Firma adı zorunludur.");

			if (!ModelState.IsValid)
			{
				await LoadCreateDropdowns(student.DepartmentId, null, model.Types);
				return View(model);
			}

			var academicId = await AssignAcademicAsync(student.DepartmentId, model.Types);

			if (academicId == null)
			{
				ModelState.AddModelError("",
					model.Types == InternshipTypes.Summer
						? "Bölümünüzde yaz stajı sorumlusu akademisyen bulunmuyor. Lütfen bölüm koordinatörünüzle iletişime geçin."
						: "Bölümünüzde kayıtlı akademisyen bulunmuyor. Lütfen yönetici ile iletişime geçin.");

				await LoadCreateDropdowns(student.DepartmentId, null, model.Types);
				return View(model);
			}

			int companyId;

			if (model.SelectedCompanyId.HasValue && model.SelectedCompanyId.Value > 0)
			{
				var existingCompany = await _context.Companies
					.FirstOrDefaultAsync(c => c.Id == model.SelectedCompanyId.Value && !c.IsDeleted);

				if (existingCompany == null)
				{
					ModelState.AddModelError("", "Seçilen firma bulunamadı.");
					await LoadCreateDropdowns(student.DepartmentId, null, model.Types);
					return View(model);
				}

				companyId = existingCompany.Id;
			}
			else
			{
				var companyName = model.CompanyName!.Trim();

				// Öğrenci firmayı dropdown'dan seçmemiş ama aynı isimde firma varsa tekrar oluşturma.
				var existingCompanyByName = await _context.Companies
					.FirstOrDefaultAsync(c =>
						!c.IsDeleted &&
						c.Name.ToLower() == companyName.ToLower());

				if (existingCompanyByName != null)
				{
					companyId = existingCompanyByName.Id;
				}
				else
				{
					var company = new Company
					{
						Name = companyName,
						Address = model.CompanyAddress ?? "",
						Phone = model.CompanyPhone ?? "",
						Email = model.CompanyEmail ?? "",
						CompanyRepresentative = model.CompanyRepresentative ?? "",
						WebAddress = model.CompanyWebAddress ?? "",
						TaxNumber = model.CompanyTaxNumber ?? "",
						CountryId = model.CompanyCountryId,
						CityId = model.CompanyCityId
					};

					_context.Companies.Add(company);
					await _context.SaveChangesAsync();

					companyId = company.Id;
				}
			}

			var internship = new Internship
			{
				StudentId = userId,
				AcademicId = academicId.Value,
				CompanyId = companyId,
				Position = model.Position,
				Department = student.Department?.Name ?? "Bölüm Belirtilmemiş",
				StartDate = model.StartDate!.Value,
				EndDate = model.EndDate!.Value,
				Description = model.Description,
				Status = InternshipStatus.Pending,
				Types = model.Types,
				InternshipMentor = model.InternshipMentor,
				InternshipMentorEmail = model.InternshipMentorEmail,
				InternshipMentorPhone = model.InternshipMentorPhone,
				IsApplicationApproved = false,
				IsContractApproved = false
			};

			_context.Internships.Add(internship);
			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Staj başvurunuz başarıyla oluşturuldu. Akademik danışmanınız otomatik olarak atandı.";
			return RedirectToAction(nameof(Index));
		}

		[Authorize(Roles = "Student,Academic")]
		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var query = _context.Internships
				.Include(i => i.Student)
				.Include(i => i.Company)
					.ThenInclude(c => c.City)
				.Include(i => i.Company)
					.ThenInclude(c => c.Country)
				.Include(i => i.Academic)
				.Include(i => i.InternshipFiles.Where(f => !f.IsDeleted))
				.Where(i => i.Id == id && !i.IsDeleted);

			if (User.IsInRole("Student"))
			{
				query = query.Where(i => i.StudentId == userId);
			}
			else if (User.IsInRole("Academic"))
			{
				query = query.Where(i => i.AcademicId == userId);
			}

			var internship = await query.FirstOrDefaultAsync();

			if (internship == null)
				return NotFound();

			return View(internship);
		}

		[Authorize(Roles = "Student")]
		[HttpGet]
		public async Task<IActionResult> Documents(int id)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var internship = await _context.Internships
				.Include(i => i.Company)
				.Include(i => i.InternshipFiles.Where(f => !f.IsDeleted))
				.FirstOrDefaultAsync(i => i.Id == id && i.StudentId == userId && !i.IsDeleted);

			if (internship == null) return NotFound();

			return View(internship);
		}

		[Authorize(Roles = "Student")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UploadDocument(int internshipId, InternshipFileType fileType, IFormFile file)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var internship = await _context.Internships
				.FirstOrDefaultAsync(i => i.Id == internshipId && i.StudentId == userId && !i.IsDeleted);

			if (internship == null) return NotFound();

			if (file == null || file.Length == 0)
			{
				TempData["ErrorMessage"] = "Lutfen bir dosya seciniz.";
				return RedirectToAction(nameof(Documents), new { id = internshipId });
			}

			// Staj defteri sadece PDF olabilir
			if (fileType == InternshipFileType.DiaryFinal)
			{
				var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
				if (ext != ".pdf")
				{
					TempData["ErrorMessage"] = "Staj defteri sadece PDF formatinda yuklenebilir.";
					return RedirectToAction(nameof(Documents), new { id = internshipId });
				}
			}

			var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

			if (!allowedExtensions.Contains(extension))
			{
				TempData["ErrorMessage"] = "Sadece PDF, DOC, DOCX, PNG, JPG, JPEG dosyalari yuklenebilir.";
				return RedirectToAction(nameof(Documents), new { id = internshipId });
			}

			// ✅ Durum kontrolü - hangi belgeler hangi durumda yüklenebilir
			bool canUpload = fileType switch
			{
				InternshipFileType.Contract =>
					internship.Status == InternshipStatus.ApplicationApproved,

				InternshipFileType.LeavePermit =>
					internship.Status == InternshipStatus.Ongoing,

				InternshipFileType.DiaryFinal =>
					internship.Status == InternshipStatus.Completed &&
					internship.EndDate.AddDays(30) >= DateTime.Today,

				_ => false
			};

			if (!canUpload)
			{
				TempData["ErrorMessage"] = "Bu belge mevcut staj durumunuzda yuklenemez.";
				return RedirectToAction(nameof(Documents), new { id = internshipId });
			}

			var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "internships", internshipId.ToString());

			if (!Directory.Exists(uploadFolder))
				Directory.CreateDirectory(uploadFolder);

			var uniqueFileName = $"{Guid.NewGuid()}{extension}";
			var fullPath = Path.Combine(uploadFolder, uniqueFileName);

			using (var stream = new FileStream(fullPath, FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}

			var internshipFile = new InternshipFile
			{
				InternshipId = internshipId,
				FileName = file.FileName,
				FilePath = $"/uploads/internships/{internshipId}/{uniqueFileName}",
				ContentType = file.ContentType,
				FileSize = file.Length,
				FileType = fileType,
				IsSentToAcademic = false  // ✅ Varsayılan: henüz gönderilmedi
			};

			_context.InternshipFiles.Add(internshipFile);

			// ✅ Sözleşme yüklenince status değişmiyor artık
			// Status ancak "Danışmana Gönder" butonuyla değişecek (SendToAcademic metodu)

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = fileType == InternshipFileType.Contract
				? "Sozlesme yuklendi. Danismaniniza gondermek icin 'Gonder' butonuna tiklayin."
				: fileType == InternshipFileType.DiaryFinal
				? "Staj defteri yuklendi. Danismaniniza gondermek icin 'Gonder' butonuna tiklayin."
				: "Belge basariyla yuklendi.";

			return RedirectToAction(nameof(Documents), new { id = internshipId });
		}

		[Authorize(Roles = "Student")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteDocument(int fileId)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var file = await _context.InternshipFiles
				.Include(f => f.Internship)
				.FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);

			if (file == null || file.Internship.StudentId != userId)
				return NotFound();

			file.IsDeleted = true;
			file.UpdatedDate = DateTime.UtcNow;

			if (file.FileType == InternshipFileType.Contract)
			{
				file.Internship.IsContractApproved = false;
				file.Internship.ContractApprovedDate = null;
				file.Internship.ContractUploadedDate = null;         
				file.Internship.Status = InternshipStatus.ApplicationApproved;
			}

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Belge silindi.";
			return RedirectToAction(nameof(Documents), new { id = file.InternshipId });
		}

		[Authorize(Roles = "Student")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SendToAcademic(int fileId)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var file = await _context.InternshipFiles
				.Include(f => f.Internship)
				.FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);

			if (file == null || file.Internship.StudentId != userId)
				return NotFound();

			// Sadece Contract ve DiaryFinal danışmana gönderilebilir
			if (file.FileType != InternshipFileType.Contract &&
				file.FileType != InternshipFileType.DiaryFinal)
			{
				TempData["ErrorMessage"] = "Bu belge türü danışmana gönderilemez.";
				return RedirectToAction(nameof(Documents), new { id = file.InternshipId });
			}

			file.IsSentToAcademic = true;
			file.SentToAcademicDate = DateTime.UtcNow;
			file.UpdatedDate = DateTime.UtcNow;

			// Sözleşme gönderilince status güncelle
			if (file.FileType == InternshipFileType.Contract)
			{
				file.Internship.ContractUploadedDate = DateTime.UtcNow;
				file.Internship.Status = InternshipStatus.ContractUploaded;
			}

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Belge danismanınıza gönderildi.";
			return RedirectToAction(nameof(Documents), new { id = file.InternshipId });
		}

		[Authorize(Roles = "Academic")]
		public async Task<IActionResult> PendingApprovals()
		{
			var academicId = int.Parse(User.FindFirst("UserId")!.Value);

			var pendingList = await _context.Internships
				   .Include(i => i.Student)
				   .Include(i => i.Company)
				   .Include(i => i.InternshipFiles.Where(f => !f.IsDeleted))
				   .Where(i => i.AcademicId == academicId &&
				   !i.IsDeleted &&
				   (i.Status == InternshipStatus.Pending ||
					i.Status == InternshipStatus.ApplicationApproved ||
					i.Status == InternshipStatus.ContractUploaded))
	   .OrderByDescending(i => i.CreatedDate)
	   .ToListAsync();

			return View(pendingList);
		}

		[HttpPost]
		[Authorize(Roles = "Academic")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ApproveApplication(int id)
		{
			var internship = await _context.Internships.FindAsync(id);
			if (internship == null) return NotFound();

			internship.IsApplicationApproved = true;
			internship.ApplicationApprovedDate = DateTime.UtcNow;
			internship.UpdatedDate = DateTime.UtcNow;
			internship.Status = InternshipStatus.ApplicationApproved; // ✅ Eklendi

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Başvuru ön onayı verildi.";
			return RedirectToAction(nameof(PendingApprovals));
		}

		[HttpPost]
		[Authorize(Roles = "Academic")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ApproveContract(int id)
		{
			var internship = await _context.Internships.FindAsync(id);
			if (internship == null) return NotFound();

			internship.IsApplicationApproved = true;
			internship.IsContractApproved = true;
			internship.ContractApprovedDate = DateTime.UtcNow;
			internship.ApprovedDate = DateTime.UtcNow;
			internship.UpdatedDate = DateTime.UtcNow;

			if (internship.StartDate.Date <= DateTime.Today)
			{
				internship.Status = InternshipStatus.Ongoing;
			}
			else
			{
				internship.Status = InternshipStatus.Approved;
			}

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Sözleşme onaylandı.";
			return RedirectToAction(nameof(PendingApprovals));
		}

		[HttpPost]
		[Authorize(Roles = "Academic")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RejectInternship(int id)
		{
			var internship = await _context.Internships.FindAsync(id);
			if (internship == null) return NotFound();

			internship.Status = InternshipStatus.Rejected;
			internship.RejectedDate = DateTime.UtcNow;
			internship.UpdatedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Staj başvurusu reddedildi.";
			return RedirectToAction(nameof(PendingApprovals));
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

		private async Task<int?> AssignAcademicAsync(int departmentId, InternshipTypes internshipType)
		{
			if (internshipType == InternshipTypes.Summer)
			{
				// Yaz stajı → sadece IsSummerInternshipResponsible = true olan akademisyenler
				// Aralarından aktif staj sayısı en az olana ata
				var responsible = await _context.Academics
					.Where(a => a.DepartmentId == departmentId
							 && a.IsSummerInternshipResponsible
							 && !a.IsDeleted)
					.Select(a => new
					{
						a.Id,
						ActiveCount = a.Internships.Count(i =>
							!i.IsDeleted &&
							i.Status != InternshipStatus.Rejected &&
							i.Status != InternshipStatus.Completed)
					})
					.OrderBy(a => a.ActiveCount)
					.FirstOrDefaultAsync();

				return responsible?.Id;
			}
			else
			{
				// İşyeri eğitimi / diğerleri → bölümdeki tüm akademisyenler
				// En az aktif stajı olan akademisyene ata
				var academic = await _context.Academics
					.Where(a => a.DepartmentId == departmentId && !a.IsDeleted)
					.Select(a => new
					{
						a.Id,
						ActiveCount = a.Internships.Count(i =>
							!i.IsDeleted &&
							i.Status != InternshipStatus.Rejected &&
							i.Status != InternshipStatus.Completed)
					})
					.OrderBy(a => a.ActiveCount)
					.FirstOrDefaultAsync();

				return academic?.Id;
			}
		}

		private async Task LoadCreateDropdowns(int departmentId, int? selectedAcademicId, InternshipTypes? selectedType)
		{
			var academics = await _context.Academics
				.Where(a => a.DepartmentId == departmentId && !a.IsDeleted)
				.Select(a => new { a.Id, a.FullName })
				.ToListAsync();

			ViewBag.AcademicId = new SelectList(academics, "Id", "FullName", selectedAcademicId);

			ViewBag.InternshipTypes = new SelectList(
				Enum.GetValues(typeof(InternshipTypes))
					.Cast<InternshipTypes>()
					.Where(x => x != InternshipTypes.Unknown)
					.Select(x => new
					{
						Id = (int)x,
						Name = x switch
						{
							InternshipTypes.Summer => "Yaz Stajı",
							InternshipTypes.LongTime => "Uzun Dönem",
							InternshipTypes.Optional => "Gönüllü",
							_ => "Bilinmiyor"
						}
					}),
				"Id",
				"Name",
				selectedType
			);

			ViewBag.Countries = await _context.Countries
				.Where(c => !c.IsDeleted)
				.OrderByDescending(c => c.Code == "TR")
				.ThenBy(c => c.Name)
				.Select(c => new { c.Id, c.Name })
				.ToListAsync();

			var trCountryId = await _context.Countries
				.Where(c => c.Code == "TR" && !c.IsDeleted)
				.Select(c => c.Id)
				.FirstOrDefaultAsync();

			ViewBag.TurkeyCountryId = trCountryId;

			ViewBag.TurkeyCities = await _context.Cities
				.Where(c => c.CountryId == trCountryId && !c.IsDeleted)
				.OrderBy(c => c.Name)
				.Select(c => new { c.Id, c.Name })
				.ToListAsync();

		}

		// Ülkeye göre şehirleri getir (AJAX)
		[HttpGet]
		[Authorize(Roles = "Student")]
		public async Task<IActionResult> GetCitiesByCountry(int countryId)
		{
			var cities = await _context.Cities
				.Where(c => c.CountryId == countryId && !c.IsDeleted)
				.OrderBy(c => c.Name)
				.Select(c => new { c.Id, c.Name })
				.ToListAsync();

			return Json(cities);
		}

		// Şehre göre firmaları getir (AJAX)
		[HttpGet]
		[Authorize(Roles = "Student")]
		public async Task<IActionResult> GetCompaniesByCity(int cityId)
		{
			var companies = await _context.Companies
				.Where(c => c.CityId == cityId && !c.IsDeleted)
				.OrderBy(c => c.Name)
				.Select(c => new { c.Id, c.Name, c.Email, c.Phone })
				.ToListAsync();

			return Json(companies);
		}

		// Firma adı kontrolü (AJAX - büyük/küçük harf duyarsız)
		[HttpGet]
		[Authorize(Roles = "Student")]
		public async Task<IActionResult> CheckCompanyName(string name, int? cityId)
		{
			if (string.IsNullOrWhiteSpace(name))
				return Json(new { exists = false });

			var query = _context.Companies
				.Where(c => !c.IsDeleted &&
							c.Name.ToLower() == name.ToLower().Trim());

			if (cityId.HasValue && cityId.Value > 0)
				query = query.Where(c => c.CityId == cityId.Value);

			var company = await query
				.Select(c => new
				{
					c.Id,
					c.Name,
					c.Address,
					c.Phone,
					c.Email,
					c.CompanyRepresentative,
					c.WebAddress,
					c.TaxNumber,
					c.CountryId,
					c.CityId
				})
				.FirstOrDefaultAsync();

			if (company != null)
				return Json(new { exists = true, company });

			return Json(new { exists = false });
		}


		[HttpGet]
		[Authorize(Roles = "Student")]
		public async Task<IActionResult> GetMapData()
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var student = await _context.Students
				.Include(s => s.Department)
				.FirstOrDefaultAsync(s => s.Id == userId && !s.IsDeleted);

			if (student == null)
				return NotFound();

			var trCountryId = await _context.Countries
				.Where(c => c.Code == "TR" && !c.IsDeleted)
				.Select(c => c.Id)
				.FirstOrDefaultAsync();

			ViewBag.TurkeyCountryId = trCountryId;

			ViewBag.TurkeyCities = await _context.Cities
				.Where(c => c.CountryId == trCountryId && !c.IsDeleted)
				.OrderBy(c => c.Name)
				.Select(c => new { c.Id, c.Name })
				.ToListAsync();

			if (trCountryId == 0)
				return Json(new List<object>());

			var departmentId = student.DepartmentId;

			var acceptedStatuses = new[]
			{
				InternshipStatus.Approved,
				InternshipStatus.Ongoing,
				InternshipStatus.Completed
			};

			var data = await _context.Cities
				.Where(city => city.CountryId == trCountryId && !city.IsDeleted)
				.OrderBy(city => city.Name)
				.Select(city => new
				{
					cityId = city.Id,
					countryId = trCountryId,
					cityName = city.Name,

					companies = _context.Companies
						.Where(company => company.CityId == city.Id && !company.IsDeleted)
						.Select(company => new
						{
							id = company.Id,
							name = company.Name,

							internshipCount = _context.Internships.Count(i =>
								i.CompanyId == company.Id &&
								!i.IsDeleted &&
								acceptedStatuses.Contains(i.Status) &&
								_context.Students.Any(s =>
									s.Id == i.StudentId &&
									!s.IsDeleted &&
									s.DepartmentId == departmentId
								)
							)
						})
						.Where(company => company.internshipCount > 0)
						.OrderByDescending(company => company.internshipCount)
						.ToList()
				})
				.ToListAsync();

			return Json(data);
		}

		[Authorize(Roles = "Student")]
		[HttpGet]
		public async Task<IActionResult> ContractForm(int id)
		{
			var userId = int.Parse(User.FindFirst("UserId")!.Value);

			var internship = await _context.Internships
				.Include(i => i.Student)
					.ThenInclude(s => s.Department)
				.Include(i => i.Student)
					.ThenInclude(s => s.University)
				.Include(i => i.Company)
					.ThenInclude(c => c.City)
				.Include(i => i.Company)
					.ThenInclude(c => c.Country)
				.Include(i => i.Academic)
				.FirstOrDefaultAsync(i =>
					i.Id == id &&
					i.StudentId == userId &&
					!i.IsDeleted);

			if (internship == null)
				return NotFound();

			if (!internship.IsApplicationApproved &&
				internship.Status != InternshipStatus.ApplicationApproved &&
				internship.Status != InternshipStatus.ContractUploaded &&
				internship.Status != InternshipStatus.Approved &&
				internship.Status != InternshipStatus.Ongoing &&
				internship.Status != InternshipStatus.Completed)
			{
				TempData["ErrorMessage"] = "Zorunlu staj formu yalnızca ön onaydan sonra indirilebilir.";
				return RedirectToAction(nameof(Index));
			}

			ViewBag.WorkDayCount = CalculateBusinessDays(internship.StartDate, internship.EndDate);
			ViewBag.EducationYear = GetEducationYear(internship.StartDate);

			return View(internship);
		}

		private int CalculateBusinessDays(DateTime startDate, DateTime endDate)
		{
			int count = 0;

			for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
			{
				if (date.DayOfWeek != DayOfWeek.Saturday &&
					date.DayOfWeek != DayOfWeek.Sunday)
				{
					count++;
				}
			}

			return count;
		}

		private string GetEducationYear(DateTime date)
		{
			int startYear = date.Month >= 9 ? date.Year : date.Year - 1;
			int endYear = startYear + 1;

			return $"{startYear}-{endYear}";
		}

		[HttpGet]
		[Authorize(Roles = "Student")]
		public async Task<IActionResult> SearchCompanies(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return Json(new List<object>());

			var keyword = name.Trim();

			var companies = await _context.Companies
				.Include(c => c.City)
				.Include(c => c.Country)
				.Where(c => !c.IsDeleted &&
							c.Name != null &&
							EF.Functions.Like(c.Name, $"%{keyword}%"))
				.OrderBy(c => c.Name)
				.Take(8)
				.Select(c => new
				{
					id = c.Id,
					name = c.Name,
					email = c.Email,
					city = c.City != null ? c.City.Name : "",
					country = c.Country != null ? c.Country.Name : ""
				})
				.ToListAsync();

			return Json(companies);
		}

		private void PrintModelStateErrors()
		{
			foreach (var item in ModelState)
			{
				foreach (var error in item.Value.Errors)
				{
					Console.WriteLine($"MODELSTATE HATASI | Alan: {item.Key} | Hata: {error.ErrorMessage}");
				}
			}
		}
	}
}