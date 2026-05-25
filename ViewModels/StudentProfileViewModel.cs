using System.ComponentModel.DataAnnotations;

namespace WebTabanliStajTakipSistemi.ViewModels
{
	public class StudentProfileViewModel
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Ad soyad zorunludur.")]
		public string FullName { get; set; }

		[Required(ErrorMessage = "E-posta zorunludur.")]
		[EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
		public string Email { get; set; }

		public string StudentNumber { get; set; }

		public string UniversityName { get; set; }
		public string DepartmentName { get; set; }

		public string? NewPassword { get; set; }

		[Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor.")]
		public string? ConfirmPassword { get; set; }
	}
}