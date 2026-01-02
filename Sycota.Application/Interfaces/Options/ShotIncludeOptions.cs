using System;

namespace Sycota.Application.Interfaces.Options
{
    [Flags]
    public enum ShotIncludeOptions
    {
        None = 0,
        SessionResult = 1 << 0,
        All = SessionResult
    }
}