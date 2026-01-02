using System;

namespace Sycota.Application.Interfaces.Options
{
    [Flags]
    public enum SessionResultIncludeOptions
    {
        None = 0,
        Shots = 1 << 0,
        ClubMember = 1 << 1,
        TrainingSession = 1 << 2,
        All = Shots | ClubMember | TrainingSession
    }
}