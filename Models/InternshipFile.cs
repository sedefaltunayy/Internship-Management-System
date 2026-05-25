using WebTabanliStajTakipSistemi.Common;
using WebTabanliStajTakipSistemi.Enums;

namespace WebTabanliStajTakipSistemi.Models
{
	public class InternshipFile : BaseEntity
	{
		public int InternshipId { get; set; }
		public string FileName { get; set; }
		public string FilePath { get; set; }
		public string ContentType { get; set; }
		public long FileSize { get; set; }
		public InternshipFileType FileType { get; set; } = InternshipFileType.Unknown;
		// Danışmana gönderildi mi?
		public bool IsSentToAcademic { get; set; } = false;
		public DateTime? SentToAcademicDate { get; set; }

		// Akademisyen notu (opsiyonel)
		public string? AcademicNote { get; set; }

		public Internship Internship { get; set; }
	}
}
