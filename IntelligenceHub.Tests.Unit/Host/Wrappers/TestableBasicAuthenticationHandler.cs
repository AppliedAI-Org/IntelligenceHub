using IntelligenceHub.Common.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace IntelligenceHub.Tests.Unit.Host.Wrappers
{
    public class TestableBasicAuthenticationHandler : BasicAuthenticationHandler
    {
        public TestableBasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptionsMonitor<AuthSettings> authSettings)
            : base(options, logger, encoder, authSettings)
        {
        }

        // Expose the protected method as public for testing purposes
        public Task<AuthenticateResult> PublicHandleAuthenticateAsync()
        {
            return base.HandleAuthenticateAsync();
        }
    }
}
