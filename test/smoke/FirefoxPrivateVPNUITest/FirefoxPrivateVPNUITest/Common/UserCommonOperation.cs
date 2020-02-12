﻿// <copyright file="UserCommonOperation.cs" company="Mozilla">
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, you can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

namespace FirefoxPrivateVPNUITest
{
    using System;
    using System.Net;
    using System.Threading;
    using FirefoxPrivateVPNUITest.Screens;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Polly;
    using RestSharp;

    /// <summary>
    /// Here are some common operations for users.
    /// </summary>
    public class UserCommonOperation
    {
        /// <summary>
        /// The user sign in steps.
        /// </summary>
        /// <param name="vpnClient">VPN session.</param>
        /// <param name="browser">Browser session.</param>
        public static void UserSignIn(FirefoxPrivateVPNSession vpnClient, BrowserSession browser)
        {
            // Verify Account Screen
            vpnClient.Session.SwitchTo();
            VerifyAccountScreen verifyAccountScreen = new VerifyAccountScreen(vpnClient.Session);
            Assert.AreEqual("Waiting for sign in and subscription confirmation...", verifyAccountScreen.GetTitle());
            Assert.AreEqual("Cancel and try again", verifyAccountScreen.GetCancelTryAgainButtonText());

            // Switch to Browser session
            browser.Session.SwitchTo();

            // Email Input page
            Thread.Sleep(TimeSpan.FromSeconds(2));
            EmailInputPage emailInputPage = new EmailInputPage(browser.Session);
            emailInputPage.InputEmail(Environment.GetEnvironmentVariable("EXISTED_USER_NAME"));
            emailInputPage.ClickContinueButton();

            // Password Input Page
            Thread.Sleep(TimeSpan.FromSeconds(2));
            PasswordInputPage passwordInputPage = new PasswordInputPage(browser.Session);
            passwordInputPage.InputPassword(Environment.GetEnvironmentVariable("EXISTED_USER_PASSWORD"));
            passwordInputPage.ClickSignInButton();
            browser.Dispose();

            // Quick Access Screen
            vpnClient.Session.SwitchTo();
            Thread.Sleep(TimeSpan.FromSeconds(2));
            QuickAccessScreen quickAccessScreen = new QuickAccessScreen(vpnClient.Session);
            Assert.AreEqual("Quick access", quickAccessScreen.GetTitle());
            Assert.AreEqual("You can quickly access Firefox Private Network from your taskbar tray", quickAccessScreen.GetSubTitle());
            Assert.AreEqual("Located next to the clock at the bottom right of your screen", quickAccessScreen.GetDescription());
            quickAccessScreen.ClickContinueButton();
        }

        /// <summary>
        /// The user sign out steps.
        /// </summary>
        /// <param name="vpnClient">VPN session.</param>
        public static void UserSignOut(FirefoxPrivateVPNSession vpnClient)
        {
            vpnClient.Session.SwitchTo();
            MainScreen mainScreen = new MainScreen(vpnClient.Session);
            mainScreen.ClickSettingsButton();
            SettingScreen settingScreen = new SettingScreen(vpnClient.Session);
            Assert.AreEqual("Settings", settingScreen.GetTitle());
            settingScreen.ClickSignOutButton();
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// Send API request to check whether user is connected or not.
        /// </summary>
        /// <returns>The API response.</returns>
        public static IRestResponse AmIMullvad()
        {
            var client = new RestClient("https://am.i.mullvad.net/connected");
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Accept-Encoding", "gzip, deflate");
            request.AddHeader("Host", "am.i.mullvad.net");
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Accept", "*/*");
            return RetryExecute(client, request);
        }

        /// <summary>
        /// Call Mullvad API to get current city.
        /// </summary>
        /// <returns>The API response.</returns>
        public static IRestResponse GetCityViaMullvad()
        {
            var client = new RestClient("https://am.i.mullvad.net/city");
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Accept-Encoding", "gzip, deflate");
            request.AddHeader("Host", "am.i.mullvad.net");
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Accept", "*/*");
            return RetryExecute(client, request);
        }

        /// <summary>
        /// User click the toggle switch to connect to VPN.
        /// </summary>
        /// <param name="vpnClient">VPN session.</param>
        /// <param name="desktop">Desktop session.</param>
        public static void ConnectVPN(FirefoxPrivateVPNSession vpnClient, DesktopSession desktop)
        {
            // Verify user is not connected to Mullvad VPN
            IRestResponse response = AmIMullvad();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(response.Content.Contains("You are not connected to Mullvad"));

            // Click VPN switch toggle and turn on VPN
            vpnClient.Session.SwitchTo();
            MainScreen mainScreen = new MainScreen(vpnClient.Session);
            mainScreen.ToggleVPNSwitch();
            Assert.AreEqual("Connecting...", mainScreen.GetTitle());
            Assert.AreEqual("You will be protected shortly", mainScreen.GetSubtitle());
            Assert.IsTrue(mainScreen.GetOnImage().Displayed);
            Assert.IsFalse(mainScreen.GetOffImage().Displayed);

            // Verify the windows notification
            Thread.Sleep(TimeSpan.FromSeconds(2));
            desktop.Session.SwitchTo();
            WindowsNotificationScreen windowsNotificationScreen = new WindowsNotificationScreen(desktop.Session);
            Assert.AreEqual("VPN is on", windowsNotificationScreen.GetTitleText());
            Assert.AreEqual("You're secure and protected.", windowsNotificationScreen.GetMessageText());
            windowsNotificationScreen.ClickDismissButton();

            vpnClient.Session.SwitchTo();
            Assert.AreEqual("VPN is on", mainScreen.GetTitle());
            Assert.IsTrue(mainScreen.GetSubtitle().Contains("Secure and protected"));

            // Verify user is connected to Mullvad VPN
            response = AmIMullvad();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(response.Content.Contains("You are connected to Mullvad"));
        }

        /// <summary>
        /// User click the toggle switch to turn off VPN.
        /// </summary>
        /// <param name="vpnClient">VPN session.</param>
        /// <param name="desktop">Desktop session.</param>
        public static void DisconnectVPN(FirefoxPrivateVPNSession vpnClient, DesktopSession desktop)
        {
            // Verify user is already connected to VPN
            IRestResponse response = AmIMullvad();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(response.Content.Contains("You are connected to Mullvad"));

            // Click VPN switch toggle and turn off VPN
            vpnClient.Session.SwitchTo();
            MainScreen mainScreen = new MainScreen(vpnClient.Session);
            mainScreen.ToggleVPNSwitch();
            Assert.AreEqual("Disconnecting...", mainScreen.GetTitle());
            Assert.AreEqual("You will be disconnected shortly", mainScreen.GetSubtitle());
            Assert.IsTrue(mainScreen.GetOffImage().Displayed);
            Assert.IsFalse(mainScreen.GetOnImage().Displayed);

            // Verify the windows notification
            Thread.Sleep(TimeSpan.FromSeconds(2));
            desktop.Session.SwitchTo();
            WindowsNotificationScreen windowsNotificationScreen = new WindowsNotificationScreen(desktop.Session);
            Assert.AreEqual("VPN is off", windowsNotificationScreen.GetTitleText());
            Assert.AreEqual("You disconnected.", windowsNotificationScreen.GetMessageText());
            windowsNotificationScreen.ClickDismissButton();

            vpnClient.Session.SwitchTo();
            Assert.AreEqual("VPN is off", mainScreen.GetTitle());
            Assert.AreEqual("Turn it on to protect your entire device", mainScreen.GetSubtitle());

            // Verify user disconnected to Mullvad VPN
            response = AmIMullvad();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(response.Content.Contains("You are not connected to Mullvad"));
        }

        /// <summary>
        /// Execute the request with retry policy.
        /// </summary>
        /// <param name="client">RestClient object.</param>
        /// <param name="request">RestRequest object.</param>
        /// <returns>The API response.</returns>
        private static IRestResponse RetryExecute(IRestClient client, IRestRequest request)
        {
            // Define retry policy
            var policy = Policy.HandleResult<IRestResponse>((response) =>
            {
                return response.StatusCode != HttpStatusCode.OK;
            }).WaitAndRetry(3, (count) => TimeSpan.FromSeconds(count));

            // Execute the request with policy
            var val = policy.ExecuteAndCapture(() =>
            {
                return client.Execute(request);
            });
            IRestResponse rr = val.Result;

            if (rr == null)
            {
                rr = new RestResponse();
                rr.Request = request;
                rr.ErrorException = val.FinalException;
            }

            return rr;
        }
    }
}
