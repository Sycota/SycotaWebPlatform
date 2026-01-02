using System;

namespace Sycota.Domain.Classes
{
    public class ShooterStatistics
    {
        public int ClubMemberId { get; set; }
        public int SessionsCount { get; set; }
        public int TotalShots { get; set; }
        public decimal AverageShot { get; set; }
        public decimal AverageSessionScore { get; set; }
        public decimal BestSessionScore { get; set; }
        public DateTime? BestSessionDate { get; set; }
    }
}