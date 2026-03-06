using System.ComponentModel.DataAnnotations;

namespace Sycota.Domain.Enums;

public enum ISSFWeaponType
{
    [Display(Name = "Въздушна Пушка")]
    AirRifle = 1,
    [Display(Name = "Въздушен Пистолет")]
    AirPistol = 2
}

