using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class DiaryFile : BaseEntity
	{
		public int InternshipDiaryId { get; set; }
		public string FileName { get; set; }
		public string FilePath { get; set; }
		public string ContentType { get; set; }
		public long FileSize { get; set; }

		public InternshipDiary InternshipDiary { get; set; }
	}
}
