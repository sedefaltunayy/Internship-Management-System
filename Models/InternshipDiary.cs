using WebTabanliStajTakipSistemi.Common;
using WebTabanliStajTakipSistemi.Enums;

namespace WebTabanliStajTakipSistemi.Models
{
	public class InternshipDiary : BaseEntity
	{
		public int InternshipId { get; set; }
		public int StudentId { get; set; }
		public DateTime WorkDate { get; set; }
		public string Title { get; set; }
		public string Content { get; set; }
		public DiaryStatus Status { get; set; }
		public DateTime? SubmittedDate { get; set; }
		public DateTime? RevisionRequestedDate { get; set; }
		public DateTime? ApprovedDate { get; set; }
		public int? ApprovedById { get; set; }

		public Academic ApprovedBy { get; set; }
		public Internship Internship { get; set; }
		public Student Student { get; set; }
		public ICollection<Comment> Comments { get; set; } = new List<Comment>();
		public ICollection<DiaryFile> DiaryFiles { get; set; } = new List<DiaryFile>();
	}
}
