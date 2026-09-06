using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Users.Queries.GetUsers;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    [Fact]
    public async Task Handle_ScopesToCurrentUsersCompany_AndPassesRoleFilterThrough()
    {
        var companyId = Guid.NewGuid();
        _currentUser.Setup(c => c.CompanyId).Returns(companyId);
        _userRepository
            .Setup(r => r.GetAllAsync(companyId, UserRole.DepartmentHead, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new User { Id = Guid.NewGuid(), FullName = "Dana Head", Email = "dana@co.com", Role = UserRole.DepartmentHead, IsActive = true, CompanyId = companyId },
            ]);

        var handler = new GetUsersQueryHandler(_userRepository.Object, _currentUser.Object);

        var result = await handler.Handle(new GetUsersQuery(UserRole.DepartmentHead), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].FullName.Should().Be("Dana Head");
        result[0].Role.Should().Be(nameof(UserRole.DepartmentHead));
        _userRepository.Verify(r => r.GetAllAsync(companyId, UserRole.DepartmentHead, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoCompanyOnCurrentUser_ThrowsForbidden()
    {
        _currentUser.Setup(c => c.CompanyId).Returns((Guid?)null);
        var handler = new GetUsersQueryHandler(_userRepository.Object, _currentUser.Object);

        var act = () => handler.Handle(new GetUsersQuery(null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
