﻿#region Copyright

// 
// Intelequia Software solutions - https://intelequia.com
// Copyright (c) 2019
// by Intelequia Software Solutions
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

#endregion

#region Usings
using DotNetNuke.Authentication.Azure.B2C.Auth;
using DotNetNuke.Authentication.Azure.B2C.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Framework;
using DotNetNuke.Instrumentation;
using DotNetNuke.Security.Membership;
using DotNetNuke.Services.Authentication;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
#endregion

namespace DotNetNuke.Authentication.Azure.B2C.Components
{
    internal class B2CController : ServiceLocator<IB2CController, B2CController>, IB2CController
    {
        #region constants, properties, etc.

        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(B2CController));
        private static Dictionary<int, OpenIdConnectConfiguration> _config = null;
        private static readonly Encoding TextEncoder = Encoding.UTF8;
        public const string AuthScheme = "Bearer";
        public string SchemeType => "JWT";

        internal static Dictionary<int, OpenIdConnectConfiguration> Config
        {
            get
            {
                if (_config == null)
                {
                    _config = new Dictionary<int, OpenIdConnectConfiguration>();
                }
                return _config;
            }
        }

        internal static OpenIdConnectConfiguration GetConfig(int portalId, AzureConfig azureB2cConfig)
        {
            if (!Config.ContainsKey(portalId))
            {
                var tokenConfigurationUrl = $"https://{azureB2cConfig.TenantName}.b2clogin.com/{azureB2cConfig.TenantId}/.well-known/openid-configuration?p={azureB2cConfig.SignUpPolicy}";
                var _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(tokenConfigurationUrl, new OpenIdConnectConfigurationRetriever());
                var _config = _configManager.GetConfigurationAsync().Result;
                Config.Add(portalId, _config);
            }
            return Config[portalId]; 
        }

        #endregion

        #region constructors / instantiators

        protected override Func<IB2CController> GetFactory()
        {
            return () => new B2CController();
        }

        #endregion

        #region interface implementation

        /// <summary>
        /// Validates the received JWT against the databas eand returns username when successful.
        /// </summary>
        public string ValidateToken(HttpRequestMessage request)
        {
            if (!B2CAuthMessageHandler.IsEnabled)
            {
                Logger.Debug(SchemeType + " is not registered/enabled in web.config file");
                return null;
            }

            var authorization = ValidateAuthHeader(request?.Headers.Authorization);
            return string.IsNullOrEmpty(authorization) ? null : ValidateAuthorizationValue(authorization);
        }

        /// <summary>
        /// Checks for Authorization header and validates it is B2C scheme. If successful, it returns the token string.
        /// </summary>
        /// <param name="authHdr">The request auhorization header.</param>
        /// <returns>The B2C passed in the request; otherwise, it returns null.</returns>
        private string ValidateAuthHeader(AuthenticationHeaderValue authHdr)
        {
            if (authHdr == null)
            {
                //if (Logger.IsDebugEnabled) Logger.Debug("Authorization header not present in the request"); // too verbose; shows in all web requests
                return null;
            }

            if (!string.Equals(authHdr.Scheme, AuthScheme, StringComparison.CurrentCultureIgnoreCase))
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Authorization header scheme in the request is not equal to " + SchemeType);
                return null;
            }

            var authorization = authHdr.Parameter;
            if (string.IsNullOrEmpty(authorization))
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Missing authorization header value in the request");
                return null;
            }

            return authorization;
        }

        private string ValidateAuthorizationValue(string authorization)
        {
            if (authorization.Contains("oauth_token="))
            {
                authorization = authorization.Split('&').FirstOrDefault(x => x.Contains("oauth_token=")).Substring("oauth_token=".Length);
            }
            var parts = authorization.Split('.');
            if (parts.Length < 3)
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Token must have [header:claims:signature] parts at least");
                return null;
            }

            var decoded = DecodeBase64(parts[0]);
            if (decoded.IndexOf("\"" + SchemeType + "\"", StringComparison.InvariantCultureIgnoreCase) < 0)            
            {
                if (Logger.IsDebugEnabled) Logger.Debug($"This is not a {SchemeType} authentication scheme.");
                return null;
            }

            var header = JsonConvert.DeserializeObject<JwtHeader>(decoded);
            if (!IsValidSchemeType(header))
                return null;


            var jwt = GetAndValidateJwt(authorization, true);
            if (jwt == null)
                return null;

            var userInfo = TryGetUser(jwt);
            var userName = userInfo?.Username;

            // Sync Azure AD B2C profile and roles once per token
            if (!string.IsNullOrEmpty(userName)) {
                var cache = DotNetNuke.Services.Cache.CachingProvider.Instance();
                if (string.IsNullOrEmpty((string) cache.GetItem($"SyncB2CToken|{authorization}"))) {
                    var azureClient = new AzureClient(userInfo.PortalID, AuthMode.Login);
                    azureClient.UpdateUserProfile(jwt);
                    cache.Insert($"SyncB2CToken|{authorization}", "OK", null, jwt.ValidTo, TimeSpan.Zero);
                }
            }

            return userName;
        }

        private UserInfo TryGetUser(JwtSecurityToken jwt)
        {
            var portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            var azureB2cConfig = new AzureConfig("AzureB2C", portalSettings.PortalId);
            if (portalSettings == null)
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Unable to retrieve portal settings");
                return null;
            }
            if (!azureB2cConfig.Enabled || !azureB2cConfig.JwtAuthEnabled)
            {
                if (Logger.IsDebugEnabled) Logger.Debug($"Azure B2C JWT auth is not enabled for portal {portalSettings.PortalId}");
                return null;
            }

            var userClaim = jwt.Claims.FirstOrDefault(x => x.Type == "sub");
            if (userClaim == null)
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Can't find 'sub' claim on token");
            }
            var userInfo = UserController.GetUserByName(portalSettings.PortalId, $"azureb2c-{userClaim.Value}");
            if (userInfo == null)
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Invalid user");
                return null;
            }

            var status = UserController.ValidateUser(userInfo, portalSettings.PortalId, false);
            var valid =
                status == UserValidStatus.VALID ||
                status == UserValidStatus.UPDATEPROFILE ||
                status == UserValidStatus.UPDATEPASSWORD;

            if (!valid && Logger.IsDebugEnabled)
            {
                Logger.Debug("Inactive user status: " + status);
                return null;
            }

            return userInfo;
        }


        private bool IsValidSchemeType(JwtHeader header)
        {
            //if (!SchemeType.Equals(header["typ"] as string, StringComparison.OrdinalIgnoreCase))
            if (!"JWT".Equals(header["typ"] as string, StringComparison.OrdinalIgnoreCase))
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Unsupported authentication scheme type " + header.Typ);
                return false;
            }

            return true;
        }

        private static string DecodeBase64(string b64Str)
        {
            // fix Base64 string padding
            var mod = b64Str.Length % 4;
            if (mod != 0) b64Str += new string('=', 4 - mod);
            return TextEncoder.GetString(Convert.FromBase64String(b64Str));
        }

        private static JwtSecurityToken GetAndValidateJwt(string rawToken, bool checkExpiry)
        {
            JwtSecurityToken jwt;
            try
            {
                jwt = new JwtSecurityToken(rawToken);
            }
            catch (Exception ex)
            {
                Logger.Error("Unable to construct JWT object from authorization value. " + ex.Message);
                return null;
            }

            if (checkExpiry)
            {
                var now = DateTime.UtcNow;
                if (now < jwt.ValidFrom || now > jwt.ValidTo)
                {
                    if (Logger.IsDebugEnabled) Logger.Debug("Token is expired");
                    return null;
                }
            }

            var portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            var azureB2cConfig = new AzureConfig("AzureB2C", portalSettings.PortalId);
            if (portalSettings == null)
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Unable to retrieve portal settings");
                return null;
            }
            if (!azureB2cConfig.Enabled || !azureB2cConfig.JwtAuthEnabled)
            {
                if (Logger.IsDebugEnabled) Logger.Debug($"Azure B2C JWT auth is not enabled for portal {portalSettings.PortalId}");
                return null;
            }

            var _config = GetConfig(portalSettings.PortalId, azureB2cConfig);
            var validAudiences = azureB2cConfig.JwtAudiences.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            if (validAudiences.Length == 0)
            {
                validAudiences = new[] { azureB2cConfig.APIKey };
            }
            
            try
            {
                // Validate token.
                var _tokenValidator = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    // App Id URI and AppId of this service application are both valid audiences.
                    ValidAudiences = validAudiences,
                    // Support Azure AD V1 and V2 endpoints.
                    ValidIssuers = new[] { _config.Issuer, $"{_config.Issuer}v2.0/" },
                    IssuerSigningKeys = _config.SigningKeys
                };

                var claimsPrincipal = _tokenValidator.ValidateToken(rawToken, validationParameters, out SecurityToken _);
            }
            catch (Exception ex)
            {
                if (Logger.IsDebugEnabled) Logger.Debug($"Error validating token: {ex}");
                return null;
            }

            return jwt;
        }

        #endregion
    }
}
