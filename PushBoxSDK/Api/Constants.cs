using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseOfCode.PushBoxSDK.Api
{
    internal static class Constants
    {
        public static readonly string Platform = "windows";

        #region urls
        // internal static readonly string ApiUrl = @"https://api.pushboxsdk.com/v1/";
        internal static readonly Uri ApiUrl = new Uri("http://10.0.0.7:3000/api/v1");
        internal static readonly string Host = @"api.pushboxsdk.com";
        #endregion

        #region JSON keys
        internal static readonly string JSONKeyHMAC = @"hmac";
        internal static readonly string JSONKeyTS = @"ts";
        internal static readonly string JSONKeyApiKey = @"app_key";
        internal static readonly string JSONKeyToken = @"push_token";
        internal static readonly string JSONKeyUid = @"uid";
        internal static readonly string JSONKeyProfileId = @"profile_identifier";
        internal static readonly string JSONKeyPlatform = @"platform";
        internal static readonly string JSONKeyOccurenceTimestamp = @"timestamp";
        internal static readonly string JSONKeyAge = @"age";
        internal static readonly string JSONKeyBirthday = @"birthday";
        internal static readonly string JSONKeyGender = @"gender";
        internal static readonly string JSONKeyEvent = @"event";
        internal static readonly string JSONKeyChannels = @"channels";
        internal static readonly string JSONKeyLocationLatitude = @"latitude";
        internal static readonly string JSONKeyLocationLongitude = @"longitude";
        internal static readonly string JSONKeySuccess = @"success";
        internal static readonly string JSONKeyMessage = @"message";
        internal static readonly string JSONKeyMessages = @"messages";
        internal static readonly string JSONKeyPushId = @"push_id";
        internal static readonly string JSONKeyPushReadTime = @"read_datetime";
        #endregion

        #region api methods
        internal static readonly string MethodSetToken = @"set_token";
        internal static readonly string MethodSetAge = @"set_age";
        internal static readonly string MethodSetBirthday = @"set_birthday";
        internal static readonly string MethodLogEvent = @"log_event";
        internal static readonly string MethodLogLocation = @"log_location";
        internal static readonly string MethodSetGender = @"set_gender";
        internal static readonly string MethodSetChannels = @"set_channels";
        internal static readonly string MethodPushInteracted = @"push_interaction";
        internal static readonly string MethodPushRead = @"push_read";
        internal static readonly string MethodInbox = @"inbox";
        #endregion

        #region local settings
        internal static readonly string LocalSettingsKeyUid = "PushBoxSDK_UID";
        internal static readonly string LocalSettingsKeyQueue = "PushBoxSDK_Queue";
        #endregion
    }
}
