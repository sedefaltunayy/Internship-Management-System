using System.ComponentModel.DataAnnotations;

namespace WebTabanliStajTakipSistemi.ViewModels
{
	public class AdminProfileViewModel
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Ad soyad zorunludur.")]
		public string FullName { get; set; }

		[Required(ErrorMessage = "E-posta zorunludur.")]
		[EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
		public string Email { get; set; }

		[Display(Name = "Yeni Şifre")]
		public string? NewPassword { get; set; }

		[Display(Name = "Yeni Şifre Tekrar")]
		[Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor.")]
		public string? ConfirmPassword { get; set; }
	}
}