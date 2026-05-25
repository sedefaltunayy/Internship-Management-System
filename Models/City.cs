using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class City : BaseEntity
	{
		public string Name { get; set; }
		public int CountryId { get; set; }

		public Country Country { get; set; }
		public ICollection<Company> Companies { get; set; } = new List<Company>();
	}
}
