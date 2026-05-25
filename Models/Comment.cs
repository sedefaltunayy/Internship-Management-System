using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class Comment : BaseEntity
	{ public int AcademicId { get; set; }
		public int InternshipDiaryId { get; set; }
		public string Description { get; set; }

		public Academic Academic { get; set; }
		public InternshipDiary InternshipDiary { get; set; }
	}
}
