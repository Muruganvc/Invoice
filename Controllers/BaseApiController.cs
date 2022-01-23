using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using poc.interfaces;

namespace poc.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        private readonly IInvoceService _service;
        private readonly IConfiguration _config;

        public string clientId { get; set; }
        public string clientsecret { get; set; }
        public string redirectUrl { get; set; }
        public string environment { get; set; }
       
        public BaseApiController(IInvoceService service, IConfiguration config)
        {
            _service = service;
            _config = config;
            (string clientId, string clientsecret, string redirectUrl, string environment) = _service.AuthKeyValues(_config);
            this.clientId = clientId;
            this.clientsecret = clientsecret;
            this.redirectUrl = redirectUrl;
            this.environment = environment;
        }
       
        public async Task<List<Claim>> GetAuthTokensAsync(string code, string realmId, IOwinContext _OwinContext)
        {
            _OwinContext.Authentication.SignOut("TempState");
            var tokenResponse = await _service.Auth2(clientId, clientsecret, redirectUrl, environment).GetBearerTokenAsync(code);
            var claims = new List<Claim>();

            claims.Add(new Claim("realmId", realmId));
            if (!string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                claims.Add(new Claim("access_token", tokenResponse.AccessToken));
                claims.Add(new Claim("access_token_expires_at", (DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn)).ToString()));
            }
            if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
            {
                claims.Add(new Claim("refresh_token", tokenResponse.RefreshToken));
                claims.Add(new Claim("refresh_token_expires_at", (DateTime.Now.AddSeconds(tokenResponse.RefreshTokenExpiresIn)).ToString()));
            }
            var id = new ClaimsIdentity(claims, "Cookies");
            _OwinContext.Authentication.SignIn(id);
            return claims;
        }
    }
}