using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using WebTabanliStajTakipSistemi.Models;


namespace WebTabanliStajTakipSistemi.Data
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{
		}

		// Veritabanı Tabloları
		public DbSet<Academic> Academics { get; set; }
		public DbSet<Admin> Admins { get; set; }
		public DbSet<Comment> Comments { get; set; }
		public DbSet<Company> Companies { get; set; }
		public DbSet<Department> Departments { get; set; }
		public DbSet<DiaryFile> DiaryFiles { get; set; }
		public DbSet<Internship> Internships { get; set; }
		public DbSet<InternshipDiary> InternshipDiaries { get; set; }
		public DbSet<InternshipFile> InternshipFiles { get; set; }
		public DbSet<SystemSettings> SystemSettings { get; set; }
		public DbSet<Student> Students { get; set; }
		public DbSet<University> Universities { get; set; }
		public DbSet<Country> Countries { get; set; }
		public DbSet<City> Cities { get; set; }


		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Akademisyen - Üniversite arasındaki cascade silmeyi kapatıyoruz
			modelBuilder.Entity<Academic>()
				.HasOne(a => a.University)
				.WithMany()
				.HasForeignKey(a => a.UniversityId)
				.OnDelete(DeleteBehavior.NoAction); // Hata veren satırı bu şekilde düzeltiyoruz

			// Öğrenci - Üniversite arasındaki cascade silmeyi kapatıyoruz
			modelBuilder.Entity<Student>()
				.HasOne(s => s.University)
				.WithMany()
				.HasForeignKey(s => s.UniversityId)
				.OnDelete(DeleteBehavior.NoAction);

			// Mevcut diğer kısıtlamaların (Internship vb.) altına bunları ekleyebilirsin
			modelBuilder.Entity<Internship>()
				.HasOne(s => s.Student)
				.WithMany(u => u.Internships)
				.HasForeignKey(s => s.StudentId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Internship>()
				.HasOne(s => s.Academic)
				.WithMany(a => a.Internships)
				.HasForeignKey(s => s.AcademicId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Comment>()
				.HasOne(c => c.InternshipDiary)
				.WithMany(d => d.Comments)
				.HasForeignKey(c => c.InternshipDiaryId)
				.OnDelete(DeleteBehavior.NoAction);

			modelBuilder.Entity<DiaryFile>()
				.HasOne(df => df.InternshipDiary)
				.WithMany(d => d.DiaryFiles)
				.HasForeignKey(df => df.InternshipDiaryId)
				.OnDelete(DeleteBehavior.NoAction);
		}

	}
}
