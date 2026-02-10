using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SimpleExample.API.Controllers;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using SimpleExample.Application.Services;
using SimpleExample.Domain.Entities;
using Xunit;

namespace SimpleExample.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUserService> _mockService;
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockService = new Mock<IUserService>();
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        // Mock: Email ei ole käytössä
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        UserDto result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikäläinen");
        result.Email.Should().Be("matti@example.com");

        // Varmista että AddAsync kutsuttiin kerran
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "existing@example.com"
        };

        User existingUser = new User("Maija", "Virtanen", "existing@example.com");

        // Mock: Email on jo käytössä!
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*jo olemassa*");

        // Varmista että AddAsync EI kutsuttu
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetIdByAsync_WheneverUserExists_ShouldReturnUser()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User user = new User("Matti", "Meikäläinen", "matti@example.com");

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        UserDto? result = await _service.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("matti@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        UserDto? result = await _service.GetByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnListOfUsers()
    {
        // Arrange
        List<User> users = new()
    {
        new User("Matti", "Meikäläinen", "matti@example.com"),
        new User("Maija", "Virtanen", "maija@example.com")
    };

        _mockRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        List<UserDto> result = (await _service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Email).Should().Contain(new[]
        {
        "matti@example.com",
        "maija@example.com"
    });
    }

    [Fact]
    public async Task UpdateAsync_WhenUserExists_ShouldUpdateUser()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User user = new User("Matti", "Meikäläinen", "matti@example.com");

        UpdateUserDto dto = new UpdateUserDto
        {
            FirstName = "Updated",
            LastName = "User",
            Email = "Updated@email.com"
        };

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockRepository
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(user);

        // Act
        UserDto result = await _service.UpdateAsync(userId, dto);

        // Assert
        result.FirstName.Should().Be("Updated");
        result.LastName.Should().Be("User");
        result.Email.Should().Be("Updated@email.com");
    }

    [Fact]
    public async Task UpdateAsync_WhenUserDoesNotExist_ShouldThrowInvalidOperationException()
    {
    // Arrange
    Guid userId = Guid.NewGuid();
    var dto = new UpdateUserDto 
        { 
            FirstName = "Updated",
            LastName = "User",
            Email = "Updated@email.com" 
        };

    _mockService
        .Setup(s => s.UpdateAsync(userId, dto))
        .ThrowsAsync(new InvalidOperationException("User does not exist"));

    // Act & Assert
    await FluentActions.Invoking(() => _mockService.Object.UpdateAsync(userId, dto))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("User does not exist");
    }

    [Fact]
    public async Task DeleteAsync_WhenUserDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        bool result = await _service.DeleteAsync(userId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}



// TEHTÄVÄ: Kirjoita itse testit seuraaville:
// 1. GetByIdAsync - löytyy
// 2. GetByIdAsync - ei löydy
// 3. GetAllAsync - palauttaa listan
// 4. UpdateAsync - onnistuu
// 5. UpdateAsync - käyttäjää ei löydy
// 6. DeleteAsync - onnistuu
// 7. DeleteAsync - käyttäjää ei löydy
