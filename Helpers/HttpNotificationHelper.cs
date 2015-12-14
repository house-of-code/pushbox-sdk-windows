using Microsoft.Phone.Notification;

namespace HouseOfCode.PushBoxSDK.Helpers
{
    public static class HttpNotificationHelper
    {
        public static string ReadBody(this HttpNotification notification)
        {
            string message;

            using (System.IO.StreamReader reader = new System.IO.StreamReader(notification.Body))
            {
                message = reader.ReadToEnd();
            }

            return message;
        }
    }
}
