﻿using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MrCMS.Entities.People;
using MrCMS.Models.Auth;
using MrCMS.Services;
using MrCMS.Services.Auth;
using MrCMS.Website;
using System;
using MrCMS.TestSupport;
using Xunit;

namespace MrCMS.Tests.Services.Auth
{
    public class ResetPasswordServiceTests : MrCMSTest
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IPasswordManagementService _passwordManagementService;
        private readonly ResetPasswordService _resetPasswordService;
        private readonly IUserLookup _userLookup;
        private readonly ILogger<ResetPasswordService> _logger;
        private readonly IGetDateTimeNow _getDateTimeNow;

        public ResetPasswordServiceTests()
        {
            _userManagementService = A.Fake<IUserManagementService>();
            _passwordManagementService = A.Fake<IPasswordManagementService>();
            _userLookup = A.Fake<IUserLookup>();
            _logger = A.Fake<ILogger<ResetPasswordService>>();
            _getDateTimeNow = A.Fake<IGetDateTimeNow>();
            _resetPasswordService = new ResetPasswordService(
                _userManagementService,
                _passwordManagementService,
                _userLookup,
                EventContext,
                _logger,
                _getDateTimeNow);
        }

        [Fact]
        public void ResetPasswordService_SetResetPassword_SetsTheResetPasswordGuid()
        {
            var user = new User();

            _resetPasswordService.SetResetPassword(user);

            user.ResetPasswordGuid.Should().HaveValue();
        }

        [Fact]
        public void ResetPasswordService_SetResetPassword_SetsTheResetPasswordExpiry()
        {
            var user = new User();

            _resetPasswordService.SetResetPassword(user);

            user.ResetPasswordExpiry.Should().HaveValue();
        }


        [Fact]
        public void ResetPasswordService_ResetPassword_WhenValidCallsSetPasswordOnTheAuthorisationService()
        {
            var guid = Guid.NewGuid();
            var user = new User
            {
                ResetPasswordExpiry = DateTime.Now.AddDays(1),
                ResetPasswordGuid = guid,
                Email = "test@example.com"
            };
            A.CallTo(() => _userLookup.GetUserByEmail("test@example.com")).Returns(user);

            const string password = "password";

            A.CallTo(() => _passwordManagementService.ValidatePassword(password, password)).Returns(true);
            _resetPasswordService.ResetPassword(new ResetPasswordViewModel(guid, user)
            {
                Password = password,
                ConfirmPassword = password,
                Email = "test@example.com"
            });

            A.CallTo(() => _passwordManagementService.SetPassword(user, password, password)).MustHaveHappened();
        }

        [Fact]
        public void ResetPasswordService_ResetPassword_ResetsThePasswordGuid()
        {
            var guid = Guid.NewGuid();
            var user = new User
            {
                ResetPasswordExpiry = DateTime.Now.AddDays(1),
                ResetPasswordGuid = guid,
                Email = "test@example.com"
            };
            A.CallTo(() => _userLookup.GetUserByEmail("test@example.com")).Returns(user);
            const string password = "password";

            A.CallTo(() => _passwordManagementService.ValidatePassword(password, password)).Returns(true);
            _resetPasswordService.ResetPassword(new ResetPasswordViewModel(guid, user)
            {
                Password = password,
                ConfirmPassword = password,
                Email = "test@example.com"
            });

            user.ResetPasswordGuid.Should().NotHaveValue();
        }

        [Fact]
        public void ResetPasswordService_ResetPassword_ResetsThePasswordExpiry()
        {
            var guid = Guid.NewGuid();
            var user = new User
            {
                ResetPasswordExpiry = DateTime.Now.AddDays(1),
                ResetPasswordGuid = guid,
                Email = "test@example.com"
            };
            A.CallTo(() => _userLookup.GetUserByEmail("test@example.com")).Returns(user);
            const string password = "password";

            A.CallTo(() => _passwordManagementService.ValidatePassword(password, password)).Returns(true);
            _resetPasswordService.ResetPassword(new ResetPasswordViewModel(guid, user)
            {
                Password = password,
                ConfirmPassword = password,
                Email = "test@example.com"
            });

            user.ResetPasswordExpiry.Should().NotHaveValue();
        }

        [Fact]
        public void ResetPasswordService_ResetPassword_ThrowsAnExceptionIfTheGuidIsNotCorrect()
        {
            var guid = Guid.NewGuid();
            var user = new User
            {
                Email = "test@example.com"
            };

            A.CallTo(() => _userLookup.GetUserByEmail("test@example.com")).Returns(user);
            const string password = "password";
            _resetPasswordService.Invoking(service =>
                service.ResetPassword(new ResetPasswordViewModel
                    (guid, user)
                    {
                        Password = password,
                        ConfirmPassword = password,
                        Email = "test@example.com"
                    })).Should().Throw<InvalidOperationException>();
        }
    }
}