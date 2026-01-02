using System;

namespace Sycota.Domain.Classes
{
    public class SessionStatistics
    {
        public int SessionResultId { get; set; }
        public decimal TotalScore { get; set; }
        public decimal AverageShot { get; set; }
        public decimal StandardDeviation { get; set; }
        public int ShotsCount { get; set; }
        public int SeriesCount { get; set; }
        public decimal BestSeriesScore { get; set; }
        public int BestSeriesIndex { get; set; }
        public DateTime SessionDate { get; set; }
    }
}