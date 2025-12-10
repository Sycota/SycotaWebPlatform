namespace Sycota.Application.Interfaces.Options
{
    [Flags]
    public enum ShooterProfileIncludeOptions
    {
        None = 0,
        ClubMember = 1 << 0,
        ClubMemberUser = 1 << 1,
        ClubMemberTrainer = 1 << 2,
        All = ClubMember | ClubMemberUser | ClubMemberTrainer
    }
}

