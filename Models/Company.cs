using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class Company : BaseEntity
	{
		public string Name { get; set; }
		public string Address { get; set; }
		public string Phone { get; set; }
		public string Email { get; set; }
		public string CompanyRepresentative { get; set; }
		public string WebAddress { get; set; }
		public int? CountryId { get; set; }
		public int? CityId { get; set; }
		public Country? Country { get; set; }
		public City? City { get; set; }
		public string? TaxNumber { get; set; }
	}
}
