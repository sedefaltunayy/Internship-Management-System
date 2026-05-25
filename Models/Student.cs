using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class Student : BaseEntity
	{
		public int DepartmentId { get; set; }
		public int UniversityId { get; set; }
		public string FullName { get; set; }
		public string Email { get; set; }
		public string StudentNumber { get; set; }
		public string Password { get; set; }
		public string? IdentityNumber { get; set; }
		public string? FatherName { get; set; }
		public string? MotherName { get; set; }
		public string? BirthPlace { get; set; }
		public DateTime? BirthDate { get; set; }
		public string? Address { get; set; }
		public string? Phone { get; set; }
		public string? MobilePhone { get; set; }

		public Department? Department { get; set; }
		public University? University { get; set; }
		public ICollection<Internship>? Internships { get; set; }
	}
}
