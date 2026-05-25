using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebTabanliStajTakipSistemi.ViewModels
{
	public class DepartmentCreateViewModel
	{
		[Required]
		public string Name { get; set; }

		[Required]
		public int? UniversityId { get; set; }

		public List<SelectListItem> Universities { get; set; } = new();
	}
}