using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class University : BaseEntity
	{
		public string Name { get; set; }
		public virtual ICollection<Department> Departments { get; set; }
	}
}
