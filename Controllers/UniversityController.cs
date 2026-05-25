using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebTabanliStajTakipSistemi.Data;
using WebTabanliStajTakipSistemi.Models;

namespace WebTabanliStajTakipSistemi.Controllers
{
	public class UniversityController : Controller
	{
		private readonly AppDbContext _context;

		public UniversityController(AppDbContext context)
		{
			_context = context;
		}

		// 1. LİSTELEME (Index)
		// Sadece silinmemiş üniversiteleri listeler
		public async Task<IActionResult> Index()
		{
			var universities = await _context.Universities
				.Where(u => !u.IsDeleted)
				.ToListAsync();
			return View(universities);
		}

		// 2. EKLEME SAYFASI (Get)
		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}

		// 3. EKLEME İŞLEMİ (Post)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Name")] University university)
		{
			if (ModelState.IsValid)
			{
				// Id int (Identity) olduğu için otomatik atanır.
				// CreatedDate BaseEntity'de UtcNow olarak varsayılan değere sahiptir.
				_context.Add(university);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(university);
		}

		// 4. DÜZENLEME (Post)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(University university)
		{
			if (ModelState.IsValid)
			{
				// Güncelleme tarihini otomatik set ediyoruz
				university.UpdatedDate = DateTime.UtcNow;

				_context.Update(university);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return RedirectToAction(nameof(Index));
		}

		// 5. SİLME (POST - Soft Delete)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var university = await _context.Universities.FindAsync(id);
			if (university != null)
			{
				// Veriyi tamamen silmiyoruz, IsDeleted alanını true yapıyoruz
				university.IsDeleted = true;
				university.UpdatedDate = DateTime.UtcNow;

				await _context.SaveChangesAsync();
			}
			return RedirectToAction(nameof(Index));
		}
	}
}