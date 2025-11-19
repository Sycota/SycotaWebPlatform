using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sycota.Domain.Entities
{
    public class PersonRole
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public int ClubId { get; set; }
        public string RoleName { get; set; }
    }
}
