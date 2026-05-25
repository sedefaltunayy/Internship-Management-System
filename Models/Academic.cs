using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class Academic : BaseEntity
	{
		public int DepartmentId { get; set; }
		public int UniversityId { get; set; }
		public string FullName { get; set; }
		public string Email { get; set; }
		public string AcademicNumber { get; set; }
		public string Password { get; set; }
		public string? Title { get; set; }
		public bool IsSummerInternshipResponsible { get; set; } = false;

		public Department Department { get; set; }
		public University University { get; set; }
		public ICollection<Internship> Internships { get; set; } = new List<Internship>();
	}
}
