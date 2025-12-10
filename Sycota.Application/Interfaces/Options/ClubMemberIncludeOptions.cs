namespace Sycota.Application.Interfaces.Options
{
    [Flags]
    public enum ClubMemberIncludeOptions
    {
        None = 0,
        User = 1 << 0,
        Club = 1 << 1,
        Trainer = 1 << 2,
        Competitors = 1 << 3,
        ShooterProfile = 1 << 4,
        All = User | Club | Trainer | Competitors | ShooterProfile
    }
}