#region license
// Copyright 2026 Utah Department of Transportation
// for IdentityTests - Utah.Udot.Atspm.IdentityTests.Controllers/AccountControllerTests.cs
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Identity.Business.Accounts;
using Identity.Controllers;
using Identity.Models.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Mail;
using Utah.Udot.Atspm.Data.Models.IdentityModels;
using Utah.Udot.Atspm.Infrastructure.Configuration;
using Utah.Udot.NetStandardToolkit.Services;
using Xunit;

namespace Utah.Udot.Atspm.IdentityTests.Controllers
{
    public class AccountControllerTests
    {
        private readonly AccountController _accountController;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<IAccountService> _accountServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;

        public AccountControllerTests()
        {
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);

            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(_userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(), null, null, null, null);
            _accountServiceMock = new Mock<IAccountService>();
            _emailServiceMock = new Mock<IEmailService>();

            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(x => x["AtspmSite"]).Returns("https://localhost");

            _accountController = new AccountController(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _accountServiceMock.Object,
                _emailServiceMock.Object,
                configurationMock.Object);

            _accountServiceMock
                .Setup(s => s.CreateUser(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(new AccountResult(StatusCodes.Status200OK, string.Empty, new List<string>(), null));

            _accountServiceMock
                .Setup(s => s.Login(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new AccountResult(StatusCodes.Status200OK, "token", new List<string>(), null));

            _signInManagerMock
                .Setup(sm => sm.SignOutAsync())
                .Returns(Task.CompletedTask);

            _emailServiceMock
                .Setup(es => es.SendEmailAsync(It.IsAny<MailMessage>()))
                .ReturnsAsync(true);
        }


        [Fact]
        public async Task Register_ValidModel_ReturnsOk()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Email = "test@example.com",
                Password = "TestPassword123!",
                Agency = "Avenue"
            };
            _accountServiceMock
                .Setup(s => s.CreateUser(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(new AccountResult(StatusCodes.Status200OK, string.Empty, new List<string>(), null));

            // Act
            var result = await _accountController.Register(model);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Register_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var model = new RegisterViewModel(); // Invalid model without required properties

            // Act
            var result = await _accountController.Register(model);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidModel_ReturnsOk()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "test@example.com",
                Password = "TestPassword123!",
                RememberMe = false
            };

            _accountServiceMock
                .Setup(s => s.Login(model.Email, model.Password, model.RememberMe))
                .ReturnsAsync(new AccountResult(StatusCodes.Status200OK, "token", new List<string>(), null));

            // Act
            var result = await _accountController.Login(model);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Login_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var model = new LoginViewModel(); // Invalid model without required properties
            _accountController.ModelState.AddModelError("Email", "Required");

            // Act
            var result = await _accountController.Login(model);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Logout_ReturnsOk()
        {
            // Act
            var result = await _accountController.Logout();

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        //[Fact]
        //public async Task ChangePassword_ValidModel_ReturnsOk()
        //{
        //    // Arrange
        //    var model = new ChangePasswordViewModel
        //    {
        //        CurrentPassword = "OldPassword123!",
        //        NewPassword = "NewPassword123!",
        //        ConfirmPassword = "NewPassword123!"
        //    };

        //    var user = new ApplicationUser();

        //    _userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
        //        .ReturnsAsync(user);

        //    _userManagerMock.Setup(um => um.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword))
        //        .ReturnsAsync(IdentityResult.Success);

        //    // Act
        //    var result = await _accountController.ChangePassword(model);

        //    // Assert
        //    Assert.IsType<OkResult>(result);
        //}

        [Fact]
        public async Task ChangePassword_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var model = new ChangePasswordViewModel(); // Invalid model without required properties
            _accountController.ModelState.AddModelError("ResetToken", "Required");

            // Act
            var result = await _accountController.ChangePassword(model);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ForgotPassword_ValidModel_ReturnsOk()
        {
            // Arrange
            var model = new ForgotPasswordViewModel
            {
                Email = "test@example.com"
            };

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };

            _userManagerMock.Setup(um => um.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            _userManagerMock.Setup(um => um.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("reset-token");

            var options = Options.Create(new IdentityConfiguration
            {
                Website = "https://localhost",
                DefaultEmailAddress = "noreply@localhost"
            });

            // Act
            var result = await _accountController.ForgotPassword(options, model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ForgotPassword_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var model = new ForgotPasswordViewModel
            {
                Email = string.Empty
            }; // Invalid model without required properties
            _accountController.ModelState.AddModelError("Email", "Required");

            // Act
            var result = await _accountController.ForgotPassword(Mock.Of<IOptions<IdentityConfiguration>>(), model);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}