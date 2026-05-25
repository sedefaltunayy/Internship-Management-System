using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebTabanliStajTakipSistemi.Data;
using WebTabanliStajTakipSistemi.Models;
using WebTabanliStajTakipSistemi.ViewModels;

namespace WebTabanliStajTakipSistemi.Controllers
{
	public class DepartmentController : Controller
	{
		private readonly AppDbContext _context;

		public DepartmentController(AppDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			var departments = await _context.Departments
				.Include(d => d.University)
				.Where(d => !d.IsDeleted)
				.ToListAsync();

			ViewBag.UniversityId = new SelectList(
				await _context.Universities
					.Where(u => !u.IsDeleted)
					.OrderBy(u => u.Name)
					.ToListAsync(),
				"Id",
				"Name"
			);

			return View(departments);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(DepartmentCreateViewModel model)
		{
			if (!ModelState.IsValid)
			{
				var departments = await _context.Departments
					.Include(d => d.University)
					.Where(d => !d.IsDeleted)
					.ToListAsync();

				ViewBag.UniversityId = new SelectList(
					await _context.Universities
						.Where(u => !u.IsDeleted)
						.OrderBy(u => u.Name)
						.ToListAsync(),
					"Id",
					"Name",
					model.UniversityId
				);

				return View("Index", departments);
			}

			var department = new Department
			{
				Name = model.Name,
				UniversityId = model.UniversityId.Value
			};

			_context.Departments.Add(department);
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Department department)
		{
			if (ModelState.IsValid)
			{
				department.UpdatedDate = DateTime.UtcNow;
				_context.Departments.Update(department);
				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var department = await _context.Departments.FindAsync(id);
			if (department != null)
			{
				department.IsDeleted = true;
				department.UpdatedDate = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(Index));
		}
	}
}