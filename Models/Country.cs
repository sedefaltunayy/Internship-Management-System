using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class Country : BaseEntity
	{
		public string Name { get; set; }
		public string? Code { get; set; }

		public ICollection<City> Cities { get; set; } = new List<City>();
		public ICollection<Company> Companies { get; set; } = new List<Company>();
	}
}
