using Moq;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Application.Services;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;

namespace Sycota.Tests.Unit.Services;

public class ClubServiceTests
{
    private readonly Mock<IClubRepository> _clubRepositoryMock;
    private readonly Mock<IClubMemberRepository> _clubMemberRepositoryMock;
    private readonly Mock<IClubJoinRequestRepository> _joinRequestRepositoryMock;
    private readonly Mock<IClubInvitationRepository> _invitationRepositoryMock;
    private readonly ClubService _sut;

    public ClubServiceTests()
    {
        _clubRepositoryMock = new Mock<IClubRepository>();
        _clubMemberRepositoryMock = new Mock<IClubMemberRepository>();
        _joinRequestRepositoryMock = new Mock<IClubJoinRequestRepository>();
        _invitationRepositoryMock = new Mock<IClubInvitationRepository>();

        _sut = new ClubService(
            _clubRepositoryMock.Object,
            _clubMemberRepositoryMock.Object,
            _joinRequestRepositoryMock.Object,
            _invitationRepositoryMock.Object
        );
    }

    #region GetClubMembersAsync Tests

    [Fact]
    public async Task GetClubMembersAsync_WhenClubDoesNotExist_ReturnsFailure()
    {
        // Arrange
        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(It.IsAny<int>(), It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync((Club?)null);

        // Act
        var result = await _sut.GetClubMembersAsync(1);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не беше намерен", result.Error);
    }

    [Fact]
    public async Task GetClubMembersAsync_WhenClubExists_ReturnsMembers()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };
        var members = new List<ClubMember>
        {
            new() { Id = 1, ClubId = 1, UserId = "user1", Role = ClubRole.Admin },
            new() { Id = 2, ClubId = 1, UserId = "user2", Role = ClubRole.Competitor }
        };

        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _clubMemberRepositoryMock.Setup(x => x.GetAllClubMembersByClubIdAsync(1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(members);

        // Act
        var result = await _sut.GetClubMembersAsync(1);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count());
    }

    #endregion

    #region GetTrainersAsync Tests

    [Fact]
    public async Task GetTrainersAsync_WhenClubDoesNotExist_ReturnsFailure()
    {
        // Arrange
        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(It.IsAny<int>(), It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync((Club?)null);

        // Act
        var result = await _sut.GetTrainersAsync(1);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не беше намерен", result.Error);
    }

    [Fact]
    public async Task GetTrainersAsync_WhenClubExists_ReturnsTrainers()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };
        var trainers = new List<ClubMember>
        {
            new() { Id = 1, ClubId = 1, UserId = "trainer1", Role = ClubRole.Trainer }
        };

        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _clubMemberRepositoryMock.Setup(x => x.GetAllTrainersByClubIdAsync(1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(trainers);

        // Act
        var result = await _sut.GetTrainersAsync(1);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data);
    }

    #endregion

    #region AddClubMemberAsync Tests

    [Fact]
    public async Task AddClubMemberAsync_WhenUserIdIsEmpty_ReturnsFailure()
    {
        // Act
        var result = await _sut.AddClubMemberAsync("", 1, ClubRole.Competitor);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("задължителен", result.Error);
    }

    [Fact]
    public async Task AddClubMemberAsync_WhenClubDoesNotExist_ReturnsFailure()
    {
        // Arrange
        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(It.IsAny<int>(), It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync((Club?)null);

        // Act
        var result = await _sut.AddClubMemberAsync("user1", 1, ClubRole.Competitor);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не беше намерен", result.Error);
    }

    [Fact]
    public async Task AddClubMemberAsync_WhenUserAlreadyMember_ReturnsFailure()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };
        var existingMember = new ClubMember { Id = 1, ClubId = 1, UserId = "user1" };

        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _clubMemberRepositoryMock.Setup(x => x.GetByUserAndClubAsync("user1", 1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(existingMember);

        // Act
        var result = await _sut.AddClubMemberAsync("user1", 1, ClubRole.Competitor);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("вече е член", result.Error);
    }

    [Fact]
    public async Task AddClubMemberAsync_WhenTrainerIdProvidedButNotTrainer_ReturnsFailure()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };
        var notATrainer = new ClubMember { Id = 2, ClubId = 1, UserId = "user2", Role = ClubRole.Competitor };

        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _clubMemberRepositoryMock.Setup(x => x.GetByUserAndClubAsync("user1", 1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync((ClubMember?)null);
        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(2, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(notATrainer);

        // Act
        var result = await _sut.AddClubMemberAsync("user1", 1, ClubRole.Competitor, trainerId: 2);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не принадлежи на треньор", result.Error);
    }

    [Fact]
    public async Task AddClubMemberAsync_WhenValid_AddsAndReturnsSuccess()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };

        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _clubMemberRepositoryMock.Setup(x => x.GetByUserAndClubAsync("user1", 1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync((ClubMember?)null);
        _clubMemberRepositoryMock.Setup(x => x.AddClubMemberAsync(It.IsAny<ClubMember>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.AddClubMemberAsync("user1", 1, ClubRole.Competitor);

        // Assert
        Assert.True(result.Success);
        _clubMemberRepositoryMock.Verify(x => x.AddClubMemberAsync(It.Is<ClubMember>(m =>
            m.UserId == "user1" && m.ClubId == 1 && m.Role == ClubRole.Competitor
        )), Times.Once);
    }

    #endregion

    #region RemoveClubMemberAsync Tests

    [Fact]
    public async Task RemoveClubMemberAsync_WhenMemberDoesNotExist_ReturnsFailure()
    {
        // Arrange
        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(It.IsAny<int>(), It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync((ClubMember?)null);

        // Act
        var result = await _sut.RemoveClubMemberAsync(1);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не беше намерен", result.Error);
    }

    [Fact]
    public async Task RemoveClubMemberAsync_WhenMemberExists_RemovesAndReturnsSuccess()
    {
        // Arrange
        var member = new ClubMember { Id = 1, ClubId = 1, UserId = "user1" };

        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(member);
        _clubMemberRepositoryMock.Setup(x => x.DeleteClubMemberAsync(member))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RemoveClubMemberAsync(1);

        // Assert
        Assert.True(result.Success);
        _clubMemberRepositoryMock.Verify(x => x.DeleteClubMemberAsync(member), Times.Once);
    }

    #endregion

    #region AssignTrainerToCompetitorAsync Tests

    [Fact]
    public async Task AssignTrainerToCompetitorAsync_WhenCompetitorDoesNotExist_ReturnsFailure()
    {
        // Arrange
        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(It.IsAny<int>(), It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync((ClubMember?)null);

        // Act
        var result = await _sut.AssignTrainerToCompetitorAsync(1, 2);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не беше намерен", result.Error);
    }

    [Fact]
    public async Task AssignTrainerToCompetitorAsync_WhenMemberNotCompetitor_ReturnsFailure()
    {
        // Arrange
        var trainer = new ClubMember { Id = 1, ClubId = 1, UserId = "trainer1", Role = ClubRole.Trainer };

        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(trainer);

        // Act
        var result = await _sut.AssignTrainerToCompetitorAsync(1, 2);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("само на състезатели", result.Error);
    }

    [Fact]
    public async Task AssignTrainerToCompetitorAsync_WhenTrainerNotInSameClub_ReturnsFailure()
    {
        // Arrange
        var competitor = new ClubMember { Id = 1, ClubId = 1, UserId = "user1", Role = ClubRole.Competitor };
        var trainer = new ClubMember { Id = 2, ClubId = 2, UserId = "trainer1", Role = ClubRole.Trainer };

        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(competitor);
        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(2, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(trainer);

        // Act
        var result = await _sut.AssignTrainerToCompetitorAsync(1, 2);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("един и същ клуб", result.Error);
    }

    [Fact]
    public async Task AssignTrainerToCompetitorAsync_WhenValid_AssignsAndReturnsSuccess()
    {
        // Arrange
        var competitor = new ClubMember { Id = 1, ClubId = 1, UserId = "user1", Role = ClubRole.Competitor };
        var trainer = new ClubMember { Id = 2, ClubId = 1, UserId = "trainer1", Role = ClubRole.Trainer };

        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(competitor);
        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(2, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(trainer);
        _clubMemberRepositoryMock.Setup(x => x.UpdateClubMemberAsync(It.IsAny<ClubMember>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.AssignTrainerToCompetitorAsync(1, 2);

        // Assert
        Assert.True(result.Success);
        _clubMemberRepositoryMock.Verify(x => x.UpdateClubMemberAsync(It.Is<ClubMember>(m =>
            m.Id == 1 && m.TrainerId == 2
        )), Times.Once);
    }

    #endregion

    #region SetAdminAsTrainerAsync Tests

    [Fact]
    public async Task SetAdminAsTrainerAsync_WhenMemberNotAdmin_ReturnsFailure()
    {
        // Arrange
        var competitor = new ClubMember { Id = 1, ClubId = 1, UserId = "user1", Role = ClubRole.Competitor };

        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(competitor);

        // Act
        var result = await _sut.SetAdminAsTrainerAsync(1, true);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Само администратори", result.Error);
    }

    [Fact]
    public async Task SetAdminAsTrainerAsync_WhenValid_UpdatesAndReturnsSuccess()
    {
        // Arrange
        var admin = new ClubMember { Id = 1, ClubId = 1, UserId = "admin1", Role = ClubRole.Admin, IsAlsoTrainer = false };

        _clubMemberRepositoryMock.Setup(x => x.GetClubMemberByIdAsync(1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(admin);
        _clubMemberRepositoryMock.Setup(x => x.UpdateClubMemberAsync(It.IsAny<ClubMember>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SetAdminAsTrainerAsync(1, true);

        // Assert
        Assert.True(result.Success);
        _clubMemberRepositoryMock.Verify(x => x.UpdateClubMemberAsync(It.Is<ClubMember>(m =>
            m.Id == 1 && m.IsAlsoTrainer == true
        )), Times.Once);
    }

    #endregion

    #region CreateJoinRequestAsync Tests

    [Fact]
    public async Task CreateJoinRequestAsync_WhenUserIdEmpty_ReturnsFailure()
    {
        // Act
        var result = await _sut.CreateJoinRequestAsync("", 1, ClubRole.Competitor);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("задължителен", result.Error);
    }

    [Fact]
    public async Task CreateJoinRequestAsync_WhenClubDoesNotExist_ReturnsFailure()
    {
        // Arrange
        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(It.IsAny<int>(), It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync((Club?)null);

        // Act
        var result = await _sut.CreateJoinRequestAsync("user1", 1, ClubRole.Competitor);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не беше намерен", result.Error);
    }

    [Fact]
    public async Task CreateJoinRequestAsync_WhenUserAlreadyMember_ReturnsFailure()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };
        var existingMember = new ClubMember { Id = 1, ClubId = 1, UserId = "user1" };

        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _clubMemberRepositoryMock.Setup(x => x.GetByUserAndClubAsync("user1", 1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(existingMember);

        // Act
        var result = await _sut.CreateJoinRequestAsync("user1", 1, ClubRole.Competitor);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("вече е член", result.Error);
    }

    [Fact]
    public async Task CreateJoinRequestAsync_WhenPendingRequestExists_ReturnsFailure()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };
        var existingRequest = new ClubJoinRequest { Id = 1, UserId = "user1", ClubId = 1, Status = MembershipRequestStatus.Pending };

        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _clubMemberRepositoryMock.Setup(x => x.GetByUserAndClubAsync("user1", 1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync((ClubMember?)null);
        _joinRequestRepositoryMock.Setup(x => x.GetPendingByUserAndClubAsync("user1", 1))
            .ReturnsAsync(existingRequest);

        // Act
        var result = await _sut.CreateJoinRequestAsync("user1", 1, ClubRole.Competitor);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("чакаща заявка", result.Error);
    }

    [Fact]
    public async Task CreateJoinRequestAsync_WhenValid_CreatesAndReturnsSuccess()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };

        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _clubMemberRepositoryMock.Setup(x => x.GetByUserAndClubAsync("user1", 1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync((ClubMember?)null);
        _joinRequestRepositoryMock.Setup(x => x.GetPendingByUserAndClubAsync("user1", 1))
            .ReturnsAsync((ClubJoinRequest?)null);
        _joinRequestRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClubJoinRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateJoinRequestAsync("user1", 1, ClubRole.Competitor, message: "Please accept me");

        // Assert
        Assert.True(result.Success);
        _joinRequestRepositoryMock.Verify(x => x.AddAsync(It.Is<ClubJoinRequest>(r =>
            r.UserId == "user1" && r.ClubId == 1 && r.RequestedRole == ClubRole.Competitor && r.Message == "Please accept me"
        )), Times.Once);
    }

    #endregion

    #region ApproveJoinRequestAsync Tests

    [Fact]
    public async Task ApproveJoinRequestAsync_WhenRequestDoesNotExist_ReturnsFailure()
    {
        // Arrange
        _joinRequestRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((ClubJoinRequest?)null);

        // Act
        var result = await _sut.ApproveJoinRequestAsync(1, "admin1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не беше намерена", result.Error);
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_WhenRequestAlreadyProcessed_ReturnsFailure()
    {
        // Arrange
        var request = new ClubJoinRequest
        {
            Id = 1,
            UserId = "user1",
            ClubId = 1,
            Status = MembershipRequestStatus.Approved
        };

        _joinRequestRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(request);

        // Act
        var result = await _sut.ApproveJoinRequestAsync(1, "admin1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("вече е обработена", result.Error);
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_WhenValid_ApprovesAndAddsMember()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };
        var request = new ClubJoinRequest
        {
            Id = 1,
            UserId = "user1",
            ClubId = 1,
            RequestedRole = ClubRole.Competitor,
            Status = MembershipRequestStatus.Pending
        };

        _joinRequestRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(request);
        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _clubMemberRepositoryMock.Setup(x => x.GetByUserAndClubAsync("user1", 1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync((ClubMember?)null);
        _clubMemberRepositoryMock.Setup(x => x.AddClubMemberAsync(It.IsAny<ClubMember>()))
            .Returns(Task.CompletedTask);
        _joinRequestRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ClubJoinRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ApproveJoinRequestAsync(1, "admin1");

        // Assert
        Assert.True(result.Success);
        _clubMemberRepositoryMock.Verify(x => x.AddClubMemberAsync(It.Is<ClubMember>(m =>
            m.UserId == "user1" && m.ClubId == 1
        )), Times.Once);
        _joinRequestRepositoryMock.Verify(x => x.UpdateAsync(It.Is<ClubJoinRequest>(r =>
            r.Status == MembershipRequestStatus.Approved && r.ProcessedById == "admin1"
        )), Times.Once);
    }

    #endregion

    #region CreateInvitationAsync Tests

    [Fact]
    public async Task CreateInvitationAsync_WhenEmailEmpty_ReturnsFailure()
    {
        // Act
        var result = await _sut.CreateInvitationAsync(1, "", ClubRole.Competitor, "admin1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("задължителен", result.Error);
    }

    [Fact]
    public async Task CreateInvitationAsync_WhenInvitationAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };
        var existingInvitation = new ClubInvitation { Id = 1, ClubId = 1, Email = "test@test.com" };

        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _invitationRepositoryMock.Setup(x => x.GetPendingByEmailAndClubAsync("test@test.com", 1))
            .ReturnsAsync(existingInvitation);

        // Act
        var result = await _sut.CreateInvitationAsync(1, "test@test.com", ClubRole.Competitor, "admin1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Вече съществува", result.Error);
    }

    [Fact]
    public async Task CreateInvitationAsync_WhenValid_CreatesAndReturnsSuccess()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };

        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _invitationRepositoryMock.Setup(x => x.GetPendingByEmailAndClubAsync("test@test.com", 1))
            .ReturnsAsync((ClubInvitation?)null);
        _invitationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClubInvitation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateInvitationAsync(1, "test@test.com", ClubRole.Competitor, "admin1", message: "Welcome!");

        // Assert
        Assert.True(result.Success);
        _invitationRepositoryMock.Verify(x => x.AddAsync(It.Is<ClubInvitation>(i =>
            i.Email == "test@test.com" && i.ClubId == 1 && i.OfferedRole == ClubRole.Competitor && i.Message == "Welcome!"
        )), Times.Once);
    }

    #endregion

    #region AcceptInvitationAsync Tests

    [Fact]
    public async Task AcceptInvitationAsync_WhenCodeEmpty_ReturnsFailure()
    {
        // Act
        var result = await _sut.AcceptInvitationAsync("", "user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("задължителен", result.Error);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WhenInvitationExpired_ReturnsFailure()
    {
        // Arrange
        var invitation = new ClubInvitation
        {
            Id = 1,
            ClubId = 1,
            Email = "test@test.com",
            InvitationCode = "ABC123",
            Status = MembershipRequestStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired
        };

        _invitationRepositoryMock.Setup(x => x.GetByCodeAsync("ABC123"))
            .ReturnsAsync(invitation);

        // Act
        var result = await _sut.AcceptInvitationAsync("ABC123", "user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("изтекла", result.Error);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WhenValid_AcceptsAndAddsMember()
    {
        // Arrange
        var club = new Club { Id = 1, Name = "Test Club" };
        var invitation = new ClubInvitation
        {
            Id = 1,
            ClubId = 1,
            Email = "test@test.com",
            InvitationCode = "ABC123",
            OfferedRole = ClubRole.Competitor,
            Status = MembershipRequestStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _invitationRepositoryMock.Setup(x => x.GetByCodeAsync("ABC123"))
            .ReturnsAsync(invitation);
        _clubRepositoryMock.Setup(x => x.GetClubByIdAsync(1, It.IsAny<ClubIncludeOptions>()))
            .ReturnsAsync(club);
        _clubMemberRepositoryMock.Setup(x => x.GetByUserAndClubAsync("user1", 1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync((ClubMember?)null);
        _clubMemberRepositoryMock.Setup(x => x.AddClubMemberAsync(It.IsAny<ClubMember>()))
            .Returns(Task.CompletedTask);
        _invitationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ClubInvitation>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.AcceptInvitationAsync("ABC123", "user1");

        // Assert
        Assert.True(result.Success);
        _clubMemberRepositoryMock.Verify(x => x.AddClubMemberAsync(It.Is<ClubMember>(m =>
            m.UserId == "user1" && m.ClubId == 1
        )), Times.Once);
        _invitationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<ClubInvitation>(i =>
            i.Status == MembershipRequestStatus.Approved && i.AcceptedByUserId == "user1"
        )), Times.Once);
    }

    #endregion

    #region UserMembershipExistsAsync Tests

    [Fact]
    public async Task UserMembershipExistsAsync_WhenMemberExists_ReturnsTrue()
    {
        // Arrange
        var member = new ClubMember { Id = 1, ClubId = 1, UserId = "user1" };
        _clubMemberRepositoryMock.Setup(x => x.GetByUserAndClubAsync("user1", 1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync(member);

        // Act
        var result = await _sut.UserMembershipExistsAsync("user1", 1);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task UserMembershipExistsAsync_WhenMemberDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _clubMemberRepositoryMock.Setup(x => x.GetByUserAndClubAsync("user1", 1, It.IsAny<ClubMemberIncludeOptions>()))
            .ReturnsAsync((ClubMember?)null);

        // Act
        var result = await _sut.UserMembershipExistsAsync("user1", 1);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.Data);
    }

    #endregion
}
