using WebTabanliStajTakipSistemi.Common;
using WebTabanliStajTakipSistemi.Enums;

namespace WebTabanliStajTakipSistemi.Models
{
	public class Internship : BaseEntity
	{
		public int StudentId { get; set; }
		public int AcademicId { get; set; }
		public int CompanyId { get; set; }

		public string Position { get; set; }
		public string Department { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public string? Description { get; set; }

		public InternshipStatus Status { get; set; }
		public InternshipTypes Types { get; set; }

		// 1. Başvuru ön onayı
		public bool IsApplicationApproved { get; set; } = false;
		public DateTime? ApplicationApprovedDate { get; set; }

		// 2. Sözleşme yükleme / onay
		public DateTime? ContractUploadedDate { get; set; }
		public bool IsContractApproved { get; set; } = false;
		public DateTime? ContractApprovedDate { get; set; }

		// Not / geri bildirim
		public string? AcademicNote { get; set; }

		public DateTime? ApprovedDate { get; set; }
		public DateTime? RejectedDate { get; set; }

		public string? InternshipMentor { get; set; }
		public string? InternshipMentorEmail { get; set; }
		public string? InternshipMentorPhone { get; set; }
		
		public Student Student { get; set; }
		public Academic Academic { get; set; }
		public Company Company { get; set; }

		public ICollection<InternshipDiary> InternshipDiaries { get; set; } = new List<InternshipDiary>();
		public ICollection<InternshipFile> InternshipFiles { get; set; } = new List<InternshipFile>();
	}
}