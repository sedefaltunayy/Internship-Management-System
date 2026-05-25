using System.ComponentModel.DataAnnotations;
using System.Drawing;
using WebTabanliStajTakipSistemi.Common;

namespace WebTabanliStajTakipSistemi.Models
{
	public class Admin : BaseEntity
	{
		public string FullName { get; set; }
		public string Email { get; set; }
		public string PasswordHash { get; set; }
	}
}
