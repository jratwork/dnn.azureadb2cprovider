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

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Web;
using DotNetNuke.Authentication.Azure.B2C.Components;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Authentication.OAuth;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Skins.Controls;
using log4net;

#endregion

namespace DotNetNuke.Authentication.Azure.B2C
{
    public partial class Login : OAuthLoginBase
    {
        private ILog _logger = LogManager.GetLogger(typeof(Login));
        protected override string AuthSystemApplicationName => "AzureB2C";

        public override bool SupportsRegistration => true;

        protected override void AddCustomProperties(NameValueCollection properties)
        {
            ((AzureClient) OAuthClient).AddCustomProperties(properties);
        }

        protected override UserData GetCurrentUser()
        {
            return OAuthClient.GetCurrentUser<AzureUserData>();
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            loginButton.Click += loginButton_Click;
            registerButton.Click += loginButton_Click;

            OAuthClient = new AzureClient(PortalId, Mode);

            loginItem.Visible = (Mode == AuthMode.Login);
            registerItem.Visible = (Mode == AuthMode.Register);


            var config = new AzureConfig("AzureB2C", PortalId);
            if (config.AutoRedirect && Request["legacy"] != "1")
                loginButton_Click(null, null);
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            // AADB2C90118: The user has forgotten their password ==> Login with forgot password policy
            if (!string.IsNullOrEmpty(Request["error"]) 
                && !string.IsNullOrEmpty(Request["error_description"]) 
                && Request["error_description"]?.IndexOf("AADB2C90118") == -1)
            {
                // AADB2C90091: The user has cancelled entering self-asserted information. 
                // User clicked on Cancel when resetting the password => Redirect to the login page
                if (Request["error_description"]?.IndexOf("AADB2C90091") > -1)
                {
                    Response.Redirect(GetLoginUrl(PortalSettings.Current, Request), true);
                }
                else
                {
                    var errorMessage = Localization.GetString("LoginError", LocalResourceFile);
                    errorMessage = string.Format(errorMessage, Request["error"], Request["error_description"]);
                    _logger.Error(errorMessage);
                    UI.Skins.Skin.AddModuleMessage(this, errorMessage, ModuleMessage.ModuleMessageType.RedError);
                }
            }           
            else
            {
                var hasVerificationCode = OAuthClient.IsCurrentService() && OAuthClient.HaveVerificationCode();
                if (!hasVerificationCode)
                {
                    if (Request["error_description"]?.IndexOf("AADB2C90118") > -1)
                    {
                        ((AzureClient)OAuthClient).Policy = AzureClient.PolicyEnum.PasswordResetPolicy;
                    }
                    AuthorisationResult result = OAuthClient.Authorize();
                    if (result == AuthorisationResult.Denied)
                    {
                        UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("PrivateConfirmationMessage", Localization.SharedResourceFile), ModuleMessage.ModuleMessageType.YellowWarning);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the login URL for the given portal from the current <paramref name="request"/>.
        /// </summary>
        /// <param name="portalSettings">The portal settings.</param>
        /// <param name="request">The request.</param>
        /// <returns>The URL for the login page</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="portalSettings"/> or <paramref name="request"/> is null.</exception>
        public static string GetLoginUrl(PortalSettings portalSettings, HttpRequest request)
        {
            if (portalSettings != null && request != null)
            {
                int tabId = portalSettings.ActiveTab.TabID;
                string controlKey = "Login";
                string returnUrl = request.RawUrl;
                if (returnUrl.IndexOf("?returnurl=", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    returnUrl = returnUrl.Substring(0, returnUrl.IndexOf("?returnurl=", StringComparison.OrdinalIgnoreCase));
                }

                returnUrl = HttpUtility.UrlEncode(returnUrl);

                if (!Null.IsNull(portalSettings.LoginTabId) && string.IsNullOrEmpty(request.QueryString["override"]))
                {
                    // user defined tab
                    controlKey = string.Empty;
                    tabId = portalSettings.LoginTabId;
                }
                else if (!Null.IsNull(portalSettings.HomeTabId))
                {
                    // portal tab
                    tabId = portalSettings.HomeTabId;
                }

                // else current tab
                return Globals.NavigateURL(tabId, controlKey, new string[] { "returnUrl=" + returnUrl });
            }

            throw new ArgumentNullException(portalSettings == null ? "portalSettings" : "request");
        }
    }
}