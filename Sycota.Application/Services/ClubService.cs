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
                return ServiceResult<IEnumerable<ClubMember>>.Fail($"Клуб с id {clubId} не беше намерен.");
            }

            var members = await _clubMemberRepository.GetAllClubMembersByClubIdAsync(clubId, include);
            return ServiceResult<IEnumerable<ClubMember>>.Ok(members);
        }

        public async Task<ServiceResult<IEnumerable<ClubMember>>> GetTrainersAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail($"Клуб с id {clubId} не беше намерен.");
            }

            var trainers = await _clubMemberRepository.GetAllTrainersByClubIdAsync(clubId, include);
            return ServiceResult<IEnumerable<ClubMember>>.Ok(trainers);
        }

        public async Task<ServiceResult<IEnumerable<ClubMember>>> GetCompetitorsAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail($"Клуб с id {clubId} не беше намерен.");
            }

            var competitors = await _clubMemberRepository.GetAllCompetitorsByClubIdAsync(clubId, include);
            return ServiceResult<IEnumerable<ClubMember>>.Ok(competitors);
        }

        public async Task<ServiceResult<IEnumerable<ClubMember>>> GetAdminsAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail($"Клуб с id {clubId} не беше намерен.");
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
                return ServiceResult<IEnumerable<ClubMember>>.Fail("Посоченият член не е треньор.");
            }

            var competitors = await _clubMemberRepository.GetCompetitorsByTrainerIdAsync(trainerId, include);
            return ServiceResult<IEnumerable<ClubMember>>.Ok(competitors);
        }

        public async Task<ServiceResult<ClubMember>> GetClubMemberAsync(int clubMemberId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            var member = await _clubMemberRepository.GetClubMemberByIdAsync(clubMemberId, include);
            if (member is null)
            {
                return ServiceResult<ClubMember>.Fail($"Член на клуб с id {clubMemberId} не беше намерен.");
            }

            return ServiceResult<ClubMember>.Ok(member);
        }

        public async Task<ServiceResult> AddClubMemberAsync(string userId, int clubId, ClubRole role, int? trainerId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult.Fail("Идентификаторът на потребителя е задължителен.");
            }

            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult.Fail($"Клуб с id {clubId} не беше намерен.");
            }

            if (await UserMembershipExistsAsync(userId, clubId) is { Success: true, Data: true })
            {
                return ServiceResult.Fail("Потребителят вече е член на този клуб.");
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
                    return ServiceResult.Fail("Посоченият идентификатор на треньор не принадлежи на треньор.");
                }

                if (trainerResult.Data.ClubId != clubId)
                {
                    return ServiceResult.Fail("Треньорът трябва да принадлежи на същия клуб като добавяния член.");
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
                return ServiceResult.Fail($"Неуспешно добавяне на член на клуба: {ex.Message}");
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
                    return ServiceResult.Fail("Треньорът трябва да е треньор в същия клуб.");
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
                return ServiceResult.Fail($"Неуспешно обновяване на член на клуба: {ex.Message}");
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
                return ServiceResult.Fail($"Неуспешно премахване на член на клуба: {ex.Message}");
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
                return ServiceResult.Fail("Треньори могат да се назначават само на състезатели.");
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
                    return ServiceResult.Fail("Избраният член не е треньор.");
                }

                if (trainer.ClubId != competitor.ClubId)
                {
                    return ServiceResult.Fail("Треньорът и състезателят трябва да са в един и същ клуб.");
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
                return ServiceResult.Fail($"Неуспешно назначаване на треньор: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<ClubMember>>> GetUnassignedCompetitorsAsync(int clubId, ClubMemberIncludeOptions include = ClubMemberIncludeOptions.None)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubMember>>.Fail($"Клуб с id {clubId} не беше намерен.");
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
                return ServiceResult.Fail("Само администратори могат да бъдат задавани като треньори чрез този метод.");
            }

            try
            {
                member.IsAlsoTrainer = isAlsoTrainer;
                await _clubMemberRepository.UpdateClubMemberAsync(member);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Неуспешно обновяване на треньорския статус на администратора: {ex.Message}");
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
                return ServiceResult.Fail("Идентификаторът на потребителя е задължителен.");
            }

            var club = await _clubRepository.GetClubByIdAsync(clubId);
            if (club is null)
            {
                return ServiceResult.Fail($"Клуб с id {clubId} не беше намерен.");
            }

            if (await UserMembershipExistsAsync(userId, clubId) is { Success: true, Data: true })
            {
                return ServiceResult.Fail("Потребителят вече е член на този клуб.");
            }

            var existingRequest = await _joinRequestRepository.GetPendingByUserAndClubAsync(userId, clubId);
            if (existingRequest is not null)
            {
                return ServiceResult.Fail("Вече имате чакаща заявка за присъединяване към този клуб.");
            }

            if (trainerId.HasValue)
            {
                var trainerResult = await GetClubMemberAsync(trainerId.Value);
                if (!trainerResult.Success || trainerResult.Data.Role != ClubRole.Trainer || trainerResult.Data.ClubId != clubId)
                {
                    return ServiceResult.Fail("Невалиден избор на треньор.");
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
                return ServiceResult.Fail($"Неуспешно създаване на заявка за присъединяване: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<ClubJoinRequest>>> GetPendingJoinRequestsAsync(int clubId)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubJoinRequest>>.Fail($"Клуб с id {clubId} не беше намерен.");
            }

            var requests = await _joinRequestRepository.GetByClubIdAsync(clubId, MembershipRequestStatus.Pending);
            return ServiceResult<IEnumerable<ClubJoinRequest>>.Ok(requests);
        }

        public async Task<ServiceResult<ClubJoinRequest>> GetJoinRequestAsync(int requestId)
        {
            var request = await _joinRequestRepository.GetByIdAsync(requestId);
            if (request is null)
            {
                return ServiceResult<ClubJoinRequest>.Fail($"Заявка за присъединяване с id {requestId} не беше намерена.");
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
                return ServiceResult.Fail("Тази заявка вече е обработена.");
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
                return ServiceResult.Fail($"Неуспешно одобряване на заявката за присъединяване: {ex.Message}");
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
                return ServiceResult.Fail("Тази заявка вече е обработена.");
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
                return ServiceResult.Fail($"Неуспешно отхвърляне на заявката за присъединяване: {ex.Message}");
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
                return ServiceResult.Fail("Имейлът е задължителен.");
            }

            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult.Fail($"Клуб с id {clubId} не беше намерен.");
            }

            var existingInvitation = await _invitationRepository.GetPendingByEmailAndClubAsync(email, clubId);
            if (existingInvitation is not null)
            {
                return ServiceResult.Fail("Вече съществува активна покана за този имейл.");
            }

            if (trainerId.HasValue)
            {
                var trainerResult = await GetClubMemberAsync(trainerId.Value);
                if (!trainerResult.Success || trainerResult.Data.Role != ClubRole.Trainer || trainerResult.Data.ClubId != clubId)
                {
                    return ServiceResult.Fail("Невалиден избор на треньор.");
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
                return ServiceResult.Fail($"Неуспешно създаване на покана: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<ClubInvitation>>> GetPendingInvitationsAsync(int clubId)
        {
            if (!await ClubExistsAsync(clubId))
            {
                return ServiceResult<IEnumerable<ClubInvitation>>.Fail($"Клуб с id {clubId} не беше намерен.");
            }

            var invitations = await _invitationRepository.GetByClubIdAsync(clubId, MembershipRequestStatus.Pending);
            return ServiceResult<IEnumerable<ClubInvitation>>.Ok(invitations);
        }

        public async Task<ServiceResult<IEnumerable<ClubInvitation>>> GetPendingInvitationsForUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<IEnumerable<ClubInvitation>>.Fail("Имейлът е задължителен.");
            }

            var invitations = await _invitationRepository.GetByEmailAsync(email, MembershipRequestStatus.Pending);
            return ServiceResult<IEnumerable<ClubInvitation>>.Ok(invitations);
        }

        public async Task<ServiceResult<ClubInvitation>> GetInvitationByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return ServiceResult<ClubInvitation>.Fail("Кодът на поканата е задължителен.");
            }

            var invitation = await _invitationRepository.GetByCodeAsync(code);
            if (invitation is null)
            {
                return ServiceResult<ClubInvitation>.Fail("Невалиден код на покана.");
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
                return ServiceResult.Fail("Тази покана вече е използвана или отменена.");
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                return ServiceResult.Fail("Тази покана е изтекла.");
            }

            if (await UserMembershipExistsAsync(userId, invitation.ClubId) is { Success: true, Data: true })
            {
                return ServiceResult.Fail("Вече сте член на този клуб.");
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
                return ServiceResult.Fail($"Неуспешно приемане на поканата: {ex.Message}");
            }
        }

        public async Task<ServiceResult> CancelInvitationAsync(int invitationId)
        {
            var invitation = await _invitationRepository.GetByIdAsync(invitationId);
            if (invitation is null)
            {
                return ServiceResult.Fail($"Покана с id {invitationId} не беше намерена.");
            }

            if (invitation.Status != MembershipRequestStatus.Pending)
            {
                return ServiceResult.Fail("Тази покана вече е използвана или отменена.");
            }

            try
            {
                invitation.Status = MembershipRequestStatus.Rejected;
                await _invitationRepository.UpdateAsync(invitation);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Неуспешно отменяне на поканата: {ex.Message}");
            }
        }
    }
}
