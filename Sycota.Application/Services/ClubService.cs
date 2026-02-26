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
        private readonly IClubJoinRequestRepository _joinRequestRepository;
        private readonly IClubInvitationRepository _invitationRepository;

        public ClubService(
            IClubRepository clubRepository, 
            IClubMemberRepository clubMemberRepository,
            IClubJoinRequestRepository joinRequestRepository,
            IClubInvitationRepository invitationRepository)
        {
            _clubRepository = clubRepository;
            _clubMemberRepository = clubMemberRepository;
            _joinRequestRepository = joinRequestRepository;
            _invitationRepository = invitationRepository;
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

            if (!trainerResult.Data.CanTrain)
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

                if (!trainerResult.Data.CanTrain)
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

                if (!trainerResult.Data.CanTrain || trainerResult.Data.ClubId != existing.ClubId)
                {
                    return ServiceResult.Fail("Trainer must be a trainer within the same club.");
                }
            }

            existing.Role = member.Role;
            existing.TrainerId = member.TrainerId;
            existing.IsAlsoTrainer = member.IsAlsoTrainer;
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

        public async Task<ServiceResult> AssignTrainerToCompetitorAsync(int competitorId, int? trainerId)
        {
            var competitorResult = await GetClubMemberAsync(competitorId, ClubMemberIncludeOptions.None);
            if (!competitorResult.Success)
            {
                return ServiceResult.Fail(competitorResult.Error);
            }

            var competitor = competitorResult.Data;
            if (competitor.Role != ClubRole.Competitor)
            {
                return ServiceResult.Fail("Can only assign trainers to competitors.");
            }

            if (trainerId.HasValue)
            {
                var trainerResult = await GetClubMemberAsync(trainerId.Value);
                if (!trainerResult.Success)
                {
                    return ServiceResult.Fail(trainerResult.Error);
                }

                var trainer = trainerResult.Data;
                if (!trainer.CanTrain)
                {
                    return ServiceResult.Fail("Selected member is not a trainer.");
                }

                if (trainer.ClubId != competitor.ClubId)
                {
                    return ServiceResult.Fail("Trainer and competitor must be in the same club.");
                }
            }

            try
            {
                competitor.TrainerId = trainerId;
                await _clubMemberRepository.UpdateClubMemberAsync(competitor);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to assign trainer: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<ClubMember>>> GetUnassignedCompetitorsAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail($"Club with id {clubId} was not found.");
            }

            var allCompetitors = await _clubMemberRepository.GetAllCompetitorsByClubIdAsync(clubId, include);
            var unassigned = allCompetitors.Where(c => c.TrainerId == null);
            return ServiceResult<IEnumerable<ClubMember>>.Ok(unassigned);
        }

        public async Task<ServiceResult> SetAdminAsTrainerAsync(int adminMemberId, bool isAlsoTrainer)
        {
            var memberResult = await GetClubMemberAsync(adminMemberId);
            if (!memberResult.Success)
            {
                return ServiceResult.Fail(memberResult.Error);
            }

            var member = memberResult.Data;
            if (member.Role != ClubRole.Admin)
            {
                return ServiceResult.Fail("Only admins can be set as trainers using this method.");
            }

            try
            {
                member.IsAlsoTrainer = isAlsoTrainer;
                await _clubMemberRepository.UpdateClubMemberAsync(member);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to update admin trainer status: {ex.Message}");
            }
        }

        private async Task<bool> ClubExistsAsync(int clubId)
        {
            var club = await _clubRepository.GetClubByIdAsync(clubId);
            return club is not null;
        }

        // Join Request methods
        public async Task<ServiceResult> CreateJoinRequestAsync(string userId, int clubId, ClubRole requestedRole, int? trainerId = null, string? message = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult.Fail("User id is required.");
            }

            var club = await _clubRepository.GetClubByIdAsync(clubId);
            if (club is null)
            {
                return ServiceResult.Fail($"Club with id {clubId} was not found.");
            }

            if (await UserMembershipExistsAsync(userId, clubId) is { Success: true, Data: true })
            {
                return ServiceResult.Fail("User is already a member of this club.");
            }

            var existingRequest = await _joinRequestRepository.GetPendingByUserAndClubAsync(userId, clubId);
            if (existingRequest is not null)
            {
                return ServiceResult.Fail("You already have a pending request to join this club.");
            }

            if (trainerId.HasValue)
            {
                var trainerResult = await GetClubMemberAsync(trainerId.Value);
                if (!trainerResult.Success || trainerResult.Data.Role != ClubRole.Trainer || trainerResult.Data.ClubId != clubId)
                {
                    return ServiceResult.Fail("Invalid trainer selected.");
                }
            }

            try
            {
                var request = new ClubJoinRequest
                {
                    UserId = userId,
                    ClubId = clubId,
                    RequestedRole = requestedRole,
                    RequestedTrainerId = trainerId,
                    Message = message,
                    Status = MembershipRequestStatus.Pending,
                    RequestedAt = DateTime.UtcNow
                };

                await _joinRequestRepository.AddAsync(request);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to create join request: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<ClubJoinRequest>>> GetPendingJoinRequestsAsync(int clubId)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubJoinRequest>>.Fail($"Club with id {clubId} was not found.");
            }

            var requests = await _joinRequestRepository.GetByClubIdAsync(clubId, MembershipRequestStatus.Pending);
            return ServiceResult<IEnumerable<ClubJoinRequest>>.Ok(requests);
        }

        public async Task<ServiceResult<ClubJoinRequest>> GetJoinRequestAsync(int requestId)
        {
            var request = await _joinRequestRepository.GetByIdAsync(requestId);
            if (request is null)
            {
                return ServiceResult<ClubJoinRequest>.Fail($"Join request with id {requestId} was not found.");
            }

            return ServiceResult<ClubJoinRequest>.Ok(request);
        }

        public async Task<ServiceResult> ApproveJoinRequestAsync(int requestId, string adminUserId)
        {
            var requestResult = await GetJoinRequestAsync(requestId);
            if (!requestResult.Success)
            {
                return ServiceResult.Fail(requestResult.Error);
            }

            var request = requestResult.Data;
            if (request.Status != MembershipRequestStatus.Pending)
            {
                return ServiceResult.Fail("This request has already been processed.");
            }

            try
            {
                // Add the user as a member
                var addResult = await AddClubMemberAsync(request.UserId, request.ClubId, request.RequestedRole, request.RequestedTrainerId);
                if (!addResult.Success)
                {
                    return addResult;
                }

                // Update the request status
                request.Status = MembershipRequestStatus.Approved;
                request.ProcessedAt = DateTime.UtcNow;
                request.ProcessedById = adminUserId;

                await _joinRequestRepository.UpdateAsync(request);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to approve join request: {ex.Message}");
            }
        }

        public async Task<ServiceResult> RejectJoinRequestAsync(int requestId, string adminUserId, string? reason = null)
        {
            var requestResult = await GetJoinRequestAsync(requestId);
            if (!requestResult.Success)
            {
                return ServiceResult.Fail(requestResult.Error);
            }

            var request = requestResult.Data;
            if (request.Status != MembershipRequestStatus.Pending)
            {
                return ServiceResult.Fail("This request has already been processed.");
            }

            try
            {
                request.Status = MembershipRequestStatus.Rejected;
                request.ProcessedAt = DateTime.UtcNow;
                request.ProcessedById = adminUserId;
                request.RejectionReason = reason;

                await _joinRequestRepository.UpdateAsync(request);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to reject join request: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> HasPendingJoinRequestAsync(string userId, int clubId)
        {
            var request = await _joinRequestRepository.GetPendingByUserAndClubAsync(userId, clubId);
            return ServiceResult<bool>.Ok(request is not null);
        }

        // Invitation methods
        public async Task<ServiceResult> CreateInvitationAsync(int clubId, string email, ClubRole offeredRole, string createdById, int? trainerId = null, string? message = null, int expirationDays = 7)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult.Fail("Email is required.");
            }

            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult.Fail($"Club with id {clubId} was not found.");
            }

            var existingInvitation = await _invitationRepository.GetPendingByEmailAndClubAsync(email, clubId);
            if (existingInvitation is not null)
            {
                return ServiceResult.Fail("An active invitation already exists for this email.");
            }

            if (trainerId.HasValue)
            {
                var trainerResult = await GetClubMemberAsync(trainerId.Value);
                if (!trainerResult.Success || trainerResult.Data.Role != ClubRole.Trainer || trainerResult.Data.ClubId != clubId)
                {
                    return ServiceResult.Fail("Invalid trainer selected.");
                }
            }

            try
            {
                var invitation = new ClubInvitation
                {
                    ClubId = clubId,
                    Email = email,
                    OfferedRole = offeredRole,
                    AssignedTrainerId = trainerId,
                    Message = message,
                    CreatedById = createdById,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
                    Status = MembershipRequestStatus.Pending
                };

                await _invitationRepository.AddAsync(invitation);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to create invitation: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<ClubInvitation>>> GetPendingInvitationsAsync(int clubId)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubInvitation>>.Fail($"Club with id {clubId} was not found.");
            }

            var invitations = await _invitationRepository.GetByClubIdAsync(clubId, MembershipRequestStatus.Pending);
            return ServiceResult<IEnumerable<ClubInvitation>>.Ok(invitations);
        }

        public async Task<ServiceResult<IEnumerable<ClubInvitation>>> GetPendingInvitationsForUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<IEnumerable<ClubInvitation>>.Fail("Email is required.");
            }

            var invitations = await _invitationRepository.GetByEmailAsync(email, MembershipRequestStatus.Pending);
            return ServiceResult<IEnumerable<ClubInvitation>>.Ok(invitations);
        }

        public async Task<ServiceResult<ClubInvitation>> GetInvitationByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return ServiceResult<ClubInvitation>.Fail("Invitation code is required.");
            }

            var invitation = await _invitationRepository.GetByCodeAsync(code);
            if (invitation is null)
            {
                return ServiceResult<ClubInvitation>.Fail("Invalid invitation code.");
            }

            return ServiceResult<ClubInvitation>.Ok(invitation);
        }

        public async Task<ServiceResult> AcceptInvitationAsync(string code, string userId)
        {
            var invitationResult = await GetInvitationByCodeAsync(code);
            if (!invitationResult.Success)
            {
                return ServiceResult.Fail(invitationResult.Error);
            }

            var invitation = invitationResult.Data;

            if (invitation.Status != MembershipRequestStatus.Pending)
            {
                return ServiceResult.Fail("This invitation has already been used or cancelled.");
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                return ServiceResult.Fail("This invitation has expired.");
            }

            if (await UserMembershipExistsAsync(userId, invitation.ClubId) is { Success: true, Data: true })
            {
                return ServiceResult.Fail("You are already a member of this club.");
            }

            try
            {
                // Add the user as a member
                var addResult = await AddClubMemberAsync(userId, invitation.ClubId, invitation.OfferedRole, invitation.AssignedTrainerId);
                if (!addResult.Success)
                {
                    return addResult;
                }

                // Update the invitation status
                invitation.Status = MembershipRequestStatus.Approved;
                invitation.AcceptedAt = DateTime.UtcNow;
                invitation.AcceptedByUserId = userId;

                await _invitationRepository.UpdateAsync(invitation);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to accept invitation: {ex.Message}");
            }
        }

        public async Task<ServiceResult> CancelInvitationAsync(int invitationId)
        {
            var invitation = await _invitationRepository.GetByIdAsync(invitationId);
            if (invitation is null)
            {
                return ServiceResult.Fail($"Invitation with id {invitationId} was not found.");
            }

            if (invitation.Status != MembershipRequestStatus.Pending)
            {
                return ServiceResult.Fail("This invitation has already been used or cancelled.");
            }

            try
            {
                invitation.Status = MembershipRequestStatus.Rejected;
                await _invitationRepository.UpdateAsync(invitation);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Unable to cancel invitation: {ex.Message}");
            }
        }
    }
}
