using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Classes;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;

namespace Sycota.Application.Services
{
    public class ClubService : IClubService
    {
        private readonly IClubRepository _clubRepository;
        private readonly IClubMemberRepository _clubMemberRepository;

        public ClubService(IClubRepository clubRepository, IClubMemberRepository clubMemberRepository)
        {
            _clubRepository = clubRepository;
            _clubMemberRepository = clubMemberRepository;
        }

        public async Task<ServiceResult<IEnumerable<ClubMember>>> GetClubMembersAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail($"Club with id {clubId} was not found.");
            }

            var members = await _clubMemberRepository.GetAllClubMembersByClubIdAsync(clubId, include);
            return ServiceResult<IEnumerable<ClubMember>>.Ok(members);
        }

        public async Task<ServiceResult<IEnumerable<ClubMember>>> GetTrainersAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail($"Club with id {clubId} was not found.");
            }

            var trainers = await _clubMemberRepository.GetAllTrainersByClubIdAsync(clubId, include);
            return ServiceResult<IEnumerable<ClubMember>>.Ok(trainers);
        }

        public async Task<ServiceResult<IEnumerable<ClubMember>>> GetCompetitorsAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail($"Club with id {clubId} was not found.");
            }

            var competitors = await _clubMemberRepository.GetAllCompetitorsByClubIdAsync(clubId, include);
            return ServiceResult<IEnumerable<ClubMember>>.Ok(competitors);
        }

        public async Task<ServiceResult<IEnumerable<ClubMember>>> GetAdminsAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail($"Club with id {clubId} was not found.");
            }

            var admins = await _clubMemberRepository.GetAllAdminsByClubIdAsync(clubId, include);
            return ServiceResult<IEnumerable<ClubMember>>.Ok(admins);
        }

        public async Task<ServiceResult<IEnumerable<ClubMember>>> GetClubMembersForTrainerAsync(int trainerId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            var trainerResult = await GetClubMemberAsync(trainerId);
            if (!trainerResult.Success)
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail(trainerResult.Error);
            }

            if (trainerResult.Data.Role != ClubRole.Trainer)
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail("Provided member is not a trainer.");
            }

            var competitors = await _clubMemberRepository.GetCompetitorsByTrainerIdAsync(trainerId, include);
            return ServiceResult<IEnumerable<ClubMember>>.Ok(competitors);
        }

        public async Task<ServiceResult<ClubMember>> GetClubMemberAsync(int clubMemberId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            var member = await _clubMemberRepository.GetClubMemberByIdAsync(clubMemberId, include);
            if (member is null)
            {
                return ServiceResult<ClubMember>.Fail($"Club member with id {clubMemberId} was not found.");
            }

            return ServiceResult<ClubMember>.Ok(member);
        }

        public async Task<ServiceResult> AddClubMemberAsync(string userId, int clubId, ClubRole role, int? trainerId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult.Fail("User id is required.");
            }

            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult.Fail($"Club with id {clubId} was not found.");
            }

            if (await UserMembershipExistsAsync(userId, clubId) is { Success: true, Data: true })
            {
                return ServiceResult.Fail("User is already a member of this club.");
            }

            if (trainerId.HasValue)
            {
                var trainerResult = await GetClubMemberAsync(trainerId.Value);
                if (!trainerResult.Success)
                {
                    return ServiceResult.Fail(trainerResult.Error);
                }

                if (trainerResult.Data.Role != ClubRole.Trainer)
                {
                    return ServiceResult.Fail("Provided trainer id does not belong to a trainer.");
                }

                if (trainerResult.Data.ClubId != clubId)
                {
                    return ServiceResult.Fail("Trainer must belong to the same club as the member being added.");
                }
            }

            try
            {
                var member = new ClubMember
                {
                    UserId = userId,
                    ClubId = clubId,
                    Role = role,
                    JoinedAt = DateTime.UtcNow,
                    TrainerId = trainerId
                };

                await _clubMemberRepository.AddClubMemberAsync(member);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to add club member: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateClubMemberAsync(ClubMember member)
        {
            var existingResult = await GetClubMemberAsync(member.Id);
            if (!existingResult.Success)
            {
                return ServiceResult.Fail(existingResult.Error);
            }

            var existing = existingResult.Data;

            if (member.TrainerId.HasValue)
            {
                var trainerResult = await GetClubMemberAsync(member.TrainerId.Value);
                if (!trainerResult.Success)
                {
                    return ServiceResult.Fail(trainerResult.Error);
                }

                if (trainerResult.Data.Role != ClubRole.Trainer || trainerResult.Data.ClubId != existing.ClubId)
                {
                    return ServiceResult.Fail("Trainer must be a trainer within the same club.");
                }
            }

            existing.Role = member.Role;
            existing.TrainerId = member.TrainerId;
            existing.ShooterProfile = member.ShooterProfile;

            try
            {
                await _clubMemberRepository.UpdateClubMemberAsync(existing);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to update club member: {ex.Message}");
            }
        }

        public async Task<ServiceResult> RemoveClubMemberAsync(int clubMemberId)
        {
            var memberResult = await GetClubMemberAsync(clubMemberId);
            if (!memberResult.Success)
            {
                return ServiceResult.Fail(memberResult.Error);
            }

            try
            {
                await _clubMemberRepository.DeleteClubMemberAsync(memberResult.Data);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to remove club member: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UserMembershipExistsAsync(string userId, int clubId)
        {
            var member = await _clubMemberRepository.GetByUserAndClubAsync(userId, clubId);
            return ServiceResult<bool>.Ok(member is not null);
        }

        private async Task<bool> ClubExistsAsync(int clubId)
        {
            var club = await _clubRepository.GetClubByIdAsync(clubId);
            return club is not null;
        }
    }
}
