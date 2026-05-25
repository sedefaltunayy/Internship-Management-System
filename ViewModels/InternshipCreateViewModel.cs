using System.ComponentModel.DataAnnotations;
using WebTabanliStajTakipSistemi.Enums;

namespace WebTabanliStajTakipSistemi.ViewModels
{
	public class InternshipCreateViewModel
	{
		[Required]
		public string Position { get; set; }

		[Required]
		public InternshipTypes Types { get; set; }

		public DateTime? StartDate { get; set; }
		
		public DateTime? EndDate { get; set; }

		[Required]
		public int AcademicId { get; set; }

		public string? Description { get; set; }

		public string? InternshipMentor { get; set; }
		public string? InternshipMentorEmail { get; set; }
		public string? InternshipMentorPhone { get; set; }

		// Var olan firma seçilecekse
		public int? SelectedCompanyId { get; set; }

		// Yeni firma girilecekse
		public string? CompanyName { get; set; }
		public string? CompanyAddress { get; set; }
		public string? CompanyPhone { get; set; }
		public string? CompanyEmail { get; set; }
		public string? CompanyRepresentative { get; set; }
		public string? CompanyWebAddress { get; set; }
		public string? CompanyTaxNumber { get; set; }

		// Firma seçimi için
		public int? SelectedCountryId { get; set; }
		public int? SelectedCityId { get; set; }

		// Yeni firma eklerken
		public int? CompanyCountryId { get; set; }
		public int? CompanyCityId { get; set; }
	}
}