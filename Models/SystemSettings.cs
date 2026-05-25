using System.ComponentModel.DataAnnotations;
using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class SystemSettings : BaseEntity
	{
		[Range(1, 10240)]
		public int MaxLoading { get; set; }

		[Required]
		[StringLength(500)]
		public string AuthorizedFileExtensions { get; set; } = string.Empty;

		public bool IsPeriod { get; set; }

		public DateTime UpdatedDate { get; set; } = DateTime.Now;
	}
}
