namespace Sycota.Application.Interfaces.Options
{
    [Flags]
    public enum TrainingSessionIncludeOptions
    {
        None = 0,
        CreatedBy = 1 << 0,
        Club = 1 << 1,
        All = CreatedBy | Club
    }
}



