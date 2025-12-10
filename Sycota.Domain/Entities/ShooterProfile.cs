using Sycota.Domain.Enums;

namespace Sycota.Domain.Entities;

public class ShooterProfile
{
    public int Id { get; set; }
    public int ClubMemberId { get; set; }
    public ISSFWeaponType? PrimaryWeapon { get; set; } // Air Rifle or Air Pistol
    public ISSFCategory? Category { get; set; } // Men, Women, Junior Men, Junior Women, etc.
    public string? ISSFLicenseNumber { get; set; }
    public DateTime? ISSFLicenseExpiryDate { get; set; }
    public string? NationalLicenseNumber { get; set; }
    public DateTime? NationalLicenseExpiryDate { get; set; }
    public string? MedicalCertificateNumber { get; set; }
    public DateTime? MedicalCertificateExpiryDate { get; set; }
 //   public decimal? PersonalBestQualification { get; set; } 
 //   public DateTime? PersonalBestQualificationDate { get; set; }
 //   public decimal? PersonalBestFinal { get; set; }
 //   public DateTime? PersonalBestFinalDate { get; set; }
    public string? AdditionalNotes { get; set; }
    public ClubMember ClubMember { get; set; } = null!;
}

