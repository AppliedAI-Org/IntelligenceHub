﻿using IntelligenceHub.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.API.DTOs;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Controllers
{
    public class ProfileControllerTests
    {
        private readonly Mock<IProfileLogic> _mockProfileLogic;
        private readonly ProfileController _controller;

        public ProfileControllerTests()
        {
            _mockProfileLogic = new Mock<IProfileLogic>();
            _controller = new ProfileController(_mockProfileLogic.Object);
        }

        [Fact]
        public async Task GetProfile_ReturnsBadRequest_WhenNameIsNullOrWhiteSpace()
        {
            // Arrange
            string name = string.Empty;

            // Act
            var result = await _controller.GetProfile(name);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid route data. Please check your input.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetProfile_ReturnsNotFound_WhenProfileNotFound()
        {
            // Arrange
            string name = "nonexistent-profile";
            _mockProfileLogic.Setup(x => x.GetProfile(name)).ReturnsAsync(APIResponseWrapper<Profile>.Failure($"No profile with the name {name} was found.", APIResponseStatusCodes.NotFound));

            // Act
            var result = await _controller.GetProfile(name);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"No profile with the name {name} was found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetProfile_ReturnsOk_WhenProfileFound()
        {
            // Arrange
            string name = "existing-profile";
            var profile = new Profile { Name = name };
            _mockProfileLogic.Setup(x => x.GetProfile(name)).ReturnsAsync(APIResponseWrapper<Profile>.Success(profile));

            // Act
            var result = await _controller.GetProfile(name);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(profile, okResult.Value);
        }

        [Fact]
        public async Task AddOrUpdateProfile_ReturnsBadRequest_WhenErrorMessageIsNotEmpty()
        {
            // Arrange
            var profile = new Profile { Name = "profile" };
            _mockProfileLogic.Setup(x => x.CreateOrUpdateProfile(profile)).ReturnsAsync(APIResponseWrapper<string>.Failure("Error creating profile", APIResponseStatusCodes.BadRequest));

            // Act
            var result = await _controller.AddOrUpdateProfile(profile);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error creating profile", badRequestResult.Value);
        }

        [Fact]
        public async Task AddOrUpdateProfile_ReturnsOk_WhenProfileIsCreatedOrUpdatedSuccessfully()
        {
            // Arrange
            var profile = new Profile { Name = "profile" };
            _mockProfileLogic.Setup(x => x.CreateOrUpdateProfile(profile)).ReturnsAsync(APIResponseWrapper<string>.Success(string.Empty));
            _mockProfileLogic.Setup(x => x.GetProfile(profile.Name)).ReturnsAsync(APIResponseWrapper<Profile>.Success(profile));

            // Act
            var result = await _controller.AddOrUpdateProfile(profile);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(profile, okResult.Value);
        }

        [Fact]
        public async Task AddProfileToTools_ReturnsBadRequest_WhenNameIsNullOrEmpty()
        {
            // Arrange
            var name = string.Empty;
            var tools = new List<string> { "Tool1" };

            // Act
            var result = await _controller.AddProfileToTools(name, tools);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request. Please check the route parameter for the profile name: ''.", badRequestResult.Value);
        }

        [Fact]
        public async Task AddProfileToTools_ReturnsBadRequest_WhenToolsIsNullOrEmpty()
        {
            // Arrange
            var name = "profile";
            List<string> tools = null;

            // Act
            var result = await _controller.AddProfileToTools(name, tools);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request. The 'Tools' property cannot be null or empty: ''.", badRequestResult.Value);
        }

        [Fact]
        public async Task AddProfileToTools_ReturnsOk_WhenToolsAreSuccessfullyAdded()
        {
            // Arrange
            var name = "profile";
            var tools = new List<string> { "Tool1" };

            _mockProfileLogic.Setup(x => x.AddProfileToTools(name, tools)).ReturnsAsync(APIResponseWrapper<List<string>>.Success(tools));
            _mockProfileLogic.Setup(x => x.GetProfileToolAssociations(name)).ReturnsAsync(APIResponseWrapper<List<string>>.Success(tools));

            // Act
            var result = await _controller.AddProfileToTools(name, tools);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(tools, okResult.Value);
        }

        [Fact]
        public async Task RemoveProfileFromTools_ReturnsOk_WhenSuccessfullyRemoved()
        {
            // Arrange
            var name = "profile";
            var tools = new List<string> { "Tool1" };
            _mockProfileLogic.Setup(x => x.DeleteProfileAssociations(name, tools)).ReturnsAsync(APIResponseWrapper<List<string>>.Success(tools));

            // Act
            var result = await _controller.RemoveProfileFromTools(name, tools);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(tools, okResult.Value);
        }

        [Fact]
        public async Task DeleteProfile_ReturnsBadRequest_WhenNameIsNullOrWhiteSpace()
        {
            // Arrange
            var name = string.Empty;

            // Act
            var result = await _controller.DeleteProfile(name);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request. Please check the route parameter for the profile name: .", badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteProfile_ReturnsNotFound_WhenProfileNotFound()
        {
            // Arrange
            string name = "nonexistent-profile";
            _mockProfileLogic.Setup(x => x.DeleteProfile(name)).ReturnsAsync(APIResponseWrapper<string>.Failure("Profile not found", APIResponseStatusCodes.NotFound));

            // Act
            var result = await _controller.DeleteProfile(name);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Profile not found", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteProfile_ReturnsNoContent_WhenSuccessfullyDeleted()
        {
            // Arrange
            string name = "profile";
            _mockProfileLogic.Setup(x => x.DeleteProfile(name)).ReturnsAsync(APIResponseWrapper<string>.Success(string.Empty));

            // Act
            var result = await _controller.DeleteProfile(name);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
        }
    }
}
