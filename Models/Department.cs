using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class Department : BaseEntity
	{
		public string Name { get; set; }

		public int UniversityId { get; set; }
		public virtual University University { get; set; }
	}
}
