﻿using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using MrCMS.Data;
using MrCMS.Entities.People;
using MrCMS.Helpers;
using MrCMS.Settings;
using MrCMS.Website;

namespace MrCMS.Services.Auth
{
    public class Log2FAPending : IVerifiedPending2FA
    {
        private readonly ISystemConfigurationProvider _configurationProvider;
        private readonly IRepository<LoginAttempt> _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public Log2FAPending(ISystemConfigurationProvider configurationProvider, IRepository<LoginAttempt> repository, IHttpContextAccessor httpContextAccessor)
        {
            _configurationProvider = configurationProvider;
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task Execute(VerifiedPending2FAEventArgs args)
        {
            var securitySettings = await _configurationProvider.GetSystemSettings<SecuritySettings>();
            if (!securitySettings.LogLoginAttempts)
                return;
            var request = _httpContextAccessor.HttpContext.Request;
            var loginAttempt = new LoginAttempt
            {
                User = args.User,
                Email = args.User?.Email,
                Status = LoginAttemptStatus.TwoFactorPending,
                IpAddress = request.GetCurrentIP(),
                UserAgent = request.UserAgent()
            };
            await _repository.Add(loginAttempt);
        }
    }
}