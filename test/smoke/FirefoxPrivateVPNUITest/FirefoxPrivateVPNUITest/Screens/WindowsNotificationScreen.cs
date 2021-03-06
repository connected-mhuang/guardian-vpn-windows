﻿// <copyright file="WindowsNotificationScreen.cs" company="Mozilla">
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, you can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

namespace FirefoxPrivateVPNUITest.Screens
{
    using System;
    using OpenQA.Selenium.Appium;
    using OpenQA.Selenium.Appium.Windows;

    /// <summary>
    /// This model is for windows notification screen.
    /// </summary>
    internal class WindowsNotificationScreen
    {
        private static readonly int TimeOut = 30000;
        private AppiumWebElement titleText;
        private AppiumWebElement messageText;
        private AppiumWebElement dismissButton;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsNotificationScreen"/> class.
        /// </summary>
        /// <param name="desktopSession">Desktop session.</param>
        public WindowsNotificationScreen(WindowsDriver<WindowsElement> desktopSession)
        {
            try
            {
                WindowsElement notification = Utils.WaitUntilFindElement(desktopSession.FindElementByAccessibilityId, "NormalToastView");
                this.titleText = Utils.WaitUntilFindElement(notification.FindElementByAccessibilityId, "TitleText");
                this.messageText = Utils.WaitUntilFindElement(notification.FindElementByAccessibilityId, "MessageText");
                this.dismissButton = Utils.WaitUntilFindElement(notification.FindElementByAccessibilityId, "DismissButton");
            }
            catch (Exception)
            {
                Utils.WaitUntilFindElement(desktopSession.FindElementByName, "Action Center", TimeOut).Click();
                var notification = Utils.WaitUntilFindElement(desktopSession.FindElementByName, "Notifications from Firefox Private Network VPN", TimeOut);
                var latestNotification = Utils.WaitUntilFindElement(notification.FindElementByClassName, "ListViewItem");
                this.titleText = Utils.WaitUntilFindElement(latestNotification.FindElementByAccessibilityId, "Title");
                this.messageText = Utils.WaitUntilFindElement(latestNotification.FindElementByAccessibilityId, "Content");
                this.dismissButton = Utils.WaitUntilFindElement(latestNotification.FindElementByAccessibilityId, "DismissButton");
            }
        }

        /// <summary>
        /// Get title on windows notification screen.
        /// </summary>
        /// <returns>The title string.</returns>
        public string GetTitleText()
        {
            return this.titleText.Text;
        }

        /// <summary>
        /// Get message text on windows notification screen.
        /// </summary>
        /// <returns>The message text.</returns>
        public string GetMessageText()
        {
            return this.messageText.Text;
        }

        /// <summary>
        /// Click the dismiss button.
        /// </summary>
        public void ClickDismissButton()
        {
            if (this.dismissButton.Displayed)
            {
                this.dismissButton.Click();
            }
        }
    }
}
