namespace Sycota.Application.Interfaces.Options
{
    [Flags]
    public enum ClubIncludeOptions
    {
        None = 0,
        CreatedBy = 1 << 0,
        Members = 1 << 1,
        TrainingSessions = 1 << 2,
        All = CreatedBy | Members | TrainingSessions
    }
}

