using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class ProfileLogicTests
    {
        private readonly Mock<IProfileRepository> _mockProfileRepository;
        private readonly Mock<IProfileToolsAssociativeRepository> _mockProfileToolsRepository;
        private readonly Mock<IToolRepository> _mockToolRepository;
        private readonly Mock<IPropertyRepository> _mockPropertyRepository;
        private readonly Mock<IValidationHandler> _mockValidationHandler;
        private readonly ProfileLogic _profileLogic;

        public ProfileLogicTests()
        {
            _mockProfileRepository = new Mock<IProfileRepository>();
            _mockProfileToolsRepository = new Mock<IProfileToolsAssociativeRepository>();
            _mockToolRepository = new Mock<IToolRepository>();
            _mockPropertyRepository = new Mock<IPropertyRepository>();
            _mockValidationHandler = new Mock<IValidationHandler>();
            _profileLogic = new ProfileLogic(
                _mockProfileRepository.Object,
                _mockProfileToolsRepository.Object,
                _mockToolRepository.Object,
                _mockPropertyRepository.Object,
                _mockValidationHandler.Object
            );
        }

        [Fact]
        public async Task GetProfile_ReturnsNull_WhenProfileDoesNotExist()
        {
            // Arrange
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((DbProfile)null);

            // Act
            var result = await _profileLogic.GetProfile("NonExistentProfile");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProfile_ReturnsProfile_WhenProfileExists()
        {
            // Arrange
            var profile = new DbProfile { Id = 1, Name = "ExistingProfile", Host = AGIServiceHosts.Azure.ToString() };
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync(profile);
            _mockProfileToolsRepository.Setup(repo => repo.GetToolAssociationsAsync(It.IsAny<int>())).ReturnsAsync(new List<DbProfileTool>());

            // Act
            var result = await _profileLogic.GetProfile("ExistingProfile");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ExistingProfile", result.Name);
        }

        [Fact]
        public async Task CreateOrUpdateProfile_ReturnsErrorMessage_WhenValidationFails()
        {
            // Arrange
            var profile = new Profile { Name = "InvalidProfile" };
            _mockValidationHandler.Setup(handler => handler.ValidateAPIProfile(It.IsAny<Profile>())).Returns("Validation Error");

            // Act
            var result = await _profileLogic.CreateOrUpdateProfile(profile);

            // Assert
            Assert.Equal("Validation Error", result);
        }

        [Fact]
        public async Task CreateOrUpdateProfile_CreatesNewProfile_WhenProfileDoesNotExist()
        {
            // Arrange
            var profile = new Profile { Name = "NewProfile" };
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((DbProfile)null);
            _mockProfileRepository.Setup(repo => repo.AddAsync(It.IsAny<DbProfile>())).ReturnsAsync(new DbProfile());

            // Act
            var result = await _profileLogic.CreateOrUpdateProfile(profile);

            // Assert
            Assert.Null(result);
            _mockProfileRepository.Verify(repo => repo.AddAsync(It.IsAny<DbProfile>()), Times.Once);
        }

        [Fact]
        public async Task DeleteProfile_ReturnsErrorMessage_WhenProfileDoesNotExist()
        {
            // Arrange
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((DbProfile)null);

            // Act
            var result = await _profileLogic.DeleteProfile("NonExistentProfile");

            // Assert
            Assert.Equal("No profile with the specified name was found. Name: 'NonExistentProfile'", result);
        }

        [Fact]
        public async Task DeleteProfile_DeletesProfile_WhenProfileExists()
        {
            // Arrange
            var profile = new DbProfile { Id = 1, Name = "ExistingProfile", Host = AGIServiceHosts.Azure.ToString() };
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync(profile);
            _mockProfileRepository.Setup(repo => repo.DeleteAsync(It.IsAny<DbProfile>())).ReturnsAsync(1);

            // Act
            var result = await _profileLogic.DeleteProfile("ExistingProfile");

            // Assert
            Assert.Null(result);
            _mockProfileRepository.Verify(repo => repo.DeleteAsync(It.IsAny<DbProfile>()), Times.Once);
        }

        [Fact]
        public async Task GetAllProfiles_ReturnsEmptyList_WhenNoProfilesExist()
        {
            // Arrange
            _mockProfileRepository.Setup(repo => repo.GetAllAsync(null, null)).ReturnsAsync(new List<DbProfile>());

            // Act
            var result = await _profileLogic.GetAllProfiles();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllProfiles_ReturnsProfiles_WhenProfilesExist()
        {
            // Arrange
            var profiles = new List<DbProfile> { new DbProfile { Id = 1, Name = "Profile1", Host = AGIServiceHosts.Azure.ToString() } };
            _mockProfileRepository.Setup(repo => repo.GetAllAsync(null, null)).ReturnsAsync(profiles);
            _mockProfileToolsRepository.Setup(repo => repo.GetToolAssociationsAsync(It.IsAny<int>())).ReturnsAsync(new List<DbProfileTool>());

            // Act
            var result = await _profileLogic.GetAllProfiles();

            // Assert
            Assert.NotEmpty(result);
            Assert.Single(result);
        }
    }
}