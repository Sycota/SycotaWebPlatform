using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sycota.Domain.Enums
{
    public enum ClubRole
    {
        [Display(Name = "Състезател")]
        Competitor,
        [Display(Name = "Треньор")]
        Trainer,
        [Display(Name = "Администратор")]
        Admin
    }
}
