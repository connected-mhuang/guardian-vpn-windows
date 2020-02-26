// <copyright file="UserCommonOperation.cs" company="Mozilla">
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, you can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

namespace FirefoxPrivateVPNUITest
{
    using System;
    using System.Diagnostics;
    using System.Net;
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
        /// <param name="disposeBrowser">Whether to dispose the browser after login.</param>
        public static void UserSignIn(FirefoxPrivateVPNSession vpnClient, BrowserSession browser, bool disposeBrowser = true)
        {
            // Verify Account Screen
            vpnClient.Session.SwitchTo();
            VerifyAccountScreen verifyAccountScreen = new VerifyAccountScreen(vpnClient.Session);
            Assert.AreEqual("Waiting for sign in and subscription confirmation...", verifyAccountScreen.GetTitle());
            Assert.AreEqual("Cancel and try again", verifyAccountScreen.GetCancelTryAgainButtonText());

            // Switch to Browser session
            browser.Session.SwitchTo();

            // Email Input page
            EmailInputPage emailInputPage = new EmailInputPage(browser.Session);
            emailInputPage.InputEmail(Environment.GetEnvironmentVariable("EXISTED_USER_NAME"));
            emailInputPage.ClickContinueButton();

            // Password Input Page
            PasswordInputPage passwordInputPage = new PasswordInputPage(browser.Session);
            passwordInputPage.InputPassword(Environment.GetEnvironmentVariable("EXISTED_USER_PASSWORD"));
            passwordInputPage.ClickSignInButton();
            if (disposeBrowser)
            {
                browser.Dispose();
            }

            // Quick Access Screen
            vpnClient.Session.SwitchTo();
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
            settingScreen.ScrollDown();
            settingScreen.ClickSignOutButton();
        }

        /// <summary>
        /// Send API request to check whether user is connected or not.
        /// </summary>
        /// <returns>The API response.</returns>
        /// <param name="expectedContent">Expected content returned from API.</param>
        public static IRestResponse AmIMullvad(string expectedContent = null)
        {
            var client = new RestClient("https://am.i.mullvad.net/connected");
            var request = new RestRequest(Method.GET);
            Func<IRestResponse, bool> condition = (res) =>
            {
                if (string.IsNullOrEmpty(expectedContent))
                {
                    return res.StatusCode != HttpStatusCode.OK;
                }

                return res.StatusCode != HttpStatusCode.OK || !res.Content.Contains(expectedContent);
            };
            IRestResponse response = RetryExecute(client, request, condition);
            return response;
        }

        /// <summary>
        /// Call Mullvad API to get current city.
        /// </summary>
        /// <returns>The API response.</returns>
        /// <param name="expectedCity">Expected city returned from API.</param>
        public static IRestResponse GetCityViaMullvad(string expectedCity = null)
        {
            var client = new RestClient("https://am.i.mullvad.net/city");
            var request = new RestRequest(Method.GET);
            Func<IRestResponse, bool> condition = (res) =>
            {
                if (string.IsNullOrEmpty(expectedCity))
                {
                    return res.StatusCode != HttpStatusCode.OK;
                }

                return res.StatusCode != HttpStatusCode.OK || !expectedCity.Contains(res.Content.Trim());
            };
            IRestResponse response = RetryExecute(client, request, condition);
            Console.WriteLine($"Mullvad API city response: {response.Content}");
            return response;
        }

        /// <summary>
        /// User click the toggle switch to connect to VPN.
        /// </summary>
        /// <param name="vpnClient">VPN session.</param>
        /// <param name="desktop">Desktop session.</param>
        public static void ConnectVPN(FirefoxPrivateVPNSession vpnClient, DesktopSession desktop)
        {
            // Click VPN switch toggle and turn on VPN
            vpnClient.Session.SwitchTo();
            MainScreen mainScreen = new MainScreen(vpnClient.Session);
            Assert.AreEqual("VPN is off", mainScreen.GetTitle());
            Assert.AreEqual("Turn it on to protect your entire device", mainScreen.GetSubtitle());
            Assert.IsTrue(mainScreen.GetOffImage().Displayed);
            Assert.IsFalse(mainScreen.GetOnImage().Displayed);
            mainScreen.ToggleVPNSwitch();

            // Verify the windows notification
            WindowsNotificationScreen windowsNotificationScreen = new WindowsNotificationScreen(desktop.Session);
            Assert.AreEqual("VPN is on", windowsNotificationScreen.GetTitleText());
            Assert.AreEqual("You're secure and protected.", windowsNotificationScreen.GetMessageText());
            windowsNotificationScreen.ClickDismissButton();

            // Verify user is connected to Mullvad VPN
            IRestResponse response = AmIMullvad("You are connected to Mullvad");
            Console.WriteLine($"After connection - Mullvad connected API response: {response.Content}");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(response.Content.Contains("You are connected to Mullvad"));

            // Verify the changes on main screen
            vpnClient.Session.SwitchTo();
            Assert.IsTrue(mainScreen.GetOnImage().Displayed);
            Assert.IsFalse(mainScreen.GetOffImage().Displayed);
            Assert.AreEqual("VPN is on", mainScreen.GetTitle());
            Assert.IsTrue(mainScreen.GetSubtitle().Contains("Secure and protected"));
        }

        /// <summary>
        /// User click the toggle switch to turn off VPN.
        /// </summary>
        /// <param name="vpnClient">VPN session.</param>
        /// <param name="desktop">Desktop session.</param>
        public static void DisconnectVPN(FirefoxPrivateVPNSession vpnClient, DesktopSession desktop)
        {
            // Click VPN switch toggle and turn off VPN
            vpnClient.Session.SwitchTo();
            MainScreen mainScreen = new MainScreen(vpnClient.Session);
            mainScreen.ToggleVPNSwitch();

            // Verify the windows notification
            desktop.Session.SwitchTo();
            WindowsNotificationScreen windowsNotificationScreen = new WindowsNotificationScreen(desktop.Session);
            Assert.AreEqual("VPN is off", windowsNotificationScreen.GetTitleText());
            Assert.AreEqual("You disconnected.", windowsNotificationScreen.GetMessageText());
            windowsNotificationScreen.ClickDismissButton();

            vpnClient.Session.SwitchTo();
            Assert.IsTrue(mainScreen.GetOffImage().Displayed);
            Assert.IsFalse(mainScreen.GetOnImage().Displayed);
            Assert.AreEqual("VPN is off", mainScreen.GetTitle());
            Assert.AreEqual("Turn it on to protect your entire device", mainScreen.GetSubtitle());

            // Verify user disconnected to Mullvad VPN
            IRestResponse response = AmIMullvad("You are not connected to Mullvad");
            Console.WriteLine($"After disconnection - Mullvad connected API response: {response.Content}");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(response.Content.Contains("You are not connected to Mullvad"));
        }

        /// <summary>
        /// Execute the request with retry policy.
        /// </summary>
        /// <param name="client">RestClient object.</param>
        /// <param name="request">RestRequest object.</param>
        /// <returns>The API response.</returns>
        private static IRestResponse RetryExecute(IRestClient client, IRestRequest request, Func<IRestResponse, bool> condition)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int retries = 0;

            // Define retry policy
            var policy = Policy.HandleResult(condition).WaitAndRetry(5, (count) => TimeSpan.FromSeconds(10));

            // Execute the request with policy
            var val = policy.ExecuteAndCapture(() =>
            {
                Console.WriteLine($"{client.BaseUrl} - sent times: {++retries}");
                var response = client.Execute(request);
                Console.WriteLine($"Response: {response.StatusCode} {response.Content}");
                return response;
            });

            IRestResponse rr = val.Result;
            if (rr == null)
            {
                rr = new RestResponse();
                rr.Request = request;
                rr.ErrorException = val.FinalException;
            }

            stopwatch.Stop();
            Console.WriteLine($"The total time to get the expected response: {stopwatch.ElapsedMilliseconds} milliseconds.");
            return rr;
        }
    }
}
