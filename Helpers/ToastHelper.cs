using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace HouseOfCode.PushBoxSDK.Helpers
{
    public static class ToastHelper
    {
        /// <summary>
        ///     Show toast notification with toast template toast02.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="body"></param>
        /// <param name="toastNavigationUriString"></param>
        public static void ShowToastNotification(string title, string body, string toastNavigationUriString)
        {
            const ToastTemplateType toastTemplate = ToastTemplateType.ToastImageAndText02;
            var toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            // Set Text
            var toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(title));
            toastTextElements[1].AppendChild(toastXml.CreateTextNode(body));

            // Set image
            // Images must be less than 200 KB in size and smaller than 1024 x 1024 pixels.
            // XmlNodeList toastImageAttributes = toastXml.GetElementsByTagName("image");
            // ((XmlElement)toastImageAttributes[0]).SetAttribute("src", "ms-appx:///Images/logo-80px-80px.png");
            // ((XmlElement)toastImageAttributes[0]).SetAttribute("alt", "logo");

            // toast duration
            var toastNode = toastXml.SelectSingleNode("/toast");
            ((XmlElement) toastNode).SetAttribute("duration", "short");

            // toast navigation
            var toastElement = (XmlElement) toastXml.SelectSingleNode("/toast");
            toastElement.SetAttribute("launch", toastNavigationUriString);

            // Create the toast notification based on the XML content you've specified.
            var toast = new ToastNotification(toastXml);

            // Send your toast notification.
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}