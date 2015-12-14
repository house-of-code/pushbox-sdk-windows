using HouseOfCode.PushBoxSDK.Api;
using HouseOfCode.PushBoxSDK.Helpers;
using Microsoft.Phone.Notification;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Windows.Foundation;
using Windows.Networking.Connectivity;

namespace HouseOfCode.PushBoxSDK
{
    public class PushBoxSDK
    {
        #region singleton

        private static PushBoxSDK _instance;
        public static PushBoxSDK Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PushBoxSDK();
                }

                return _instance;
            }
        }

        private PushBoxSDK()
        {
            Logger = new Logger("PushBoxSDK");
        }

        #endregion

        private Api ApiInstance;
        public ILogger Logger;
        private bool IsSetup = false;

        public event EventHandler<OnPushEventArgs> OnPush;
        public event EventHandler<NotificationChannelErrorEventArgs> OnPushError;
        public event EventHandler<OnRequestErrorEventArgs> OnRequestError;
        public event EventHandler<OnRequestSuccessEventArgs> OnRequestSuccess;
        public event EventHandler<OnMessagesReceivedEventArgs> OnMessagesReceived;

        internal void RequestError(OnRequestErrorEventArgs eventArgs)
        {
            if (OnRequestError != null)
            {
                OnRequestError(this, eventArgs);
            }
        }

        internal void RequestSuccess(OnRequestSuccessEventArgs eventArgs)
        {
            if (OnRequestSuccess != null)
            {
                OnRequestSuccess(this, eventArgs);
            }
        }

        internal void MessagesReceived(List<PushBoxMessage> messages)
        {
            if (OnMessagesReceived != null)
            {
                OnMessagesReceived(this, new OnMessagesReceivedEventArgs(messages));
            }
        }

        private void TriggerPush(NavigatingCancelEventArgs e)
        {
            if (OnPush != null)
            {
                var payload = GetPayload(e.Uri);

                var message = MessageFromPayload(payload);

                if (message.HasValue)
                {
                    TriggerPush(message.Value);
                }
            }
            else
            {
                Logger.Warn("Did receive push, but no OnPush handlers registered.");
            }
        }

        private void TriggerPush(PushBoxMessage message)
        {
            var eventArgs = new OnPushEventArgs(message);
            OnPush(this, eventArgs);
            ApiInstance.LogPushInteracted(message.Id);
        }

        /// <summary>
        /// Initialize sdk with api credentials.
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="apiSecret"></param>
        public void Initialize(string apiKey, string apiSecret)
        {
            Logger.Debug("Setting up...");
            Instance.ApiInstance = new Api(apiKey, apiSecret, new Logger("Api", Logger.Level));
            Instance.Start();
        }

        /// <summary>
        /// Handler for navigation events. Register with Application Root Frame.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            Logger.Debugf("Got navigation event {0}, isCancelable? {1}", e.Uri, e.IsCancelable);
            if (e.IsCancelable && e.Uri.isPushBoxUri())
            {
                Logger.Debug("Cancelling and handling navigation...");
                TriggerPush(e);

                e.Cancel = true;
            }
        }



        /// <summary>
        /// Api has been setup, start registering channel/setting push token
        /// </summary>
        private void Start()
        {
            SetupMPNSChannel();
            // Subscribe to network connectivity events
            SetupNetworkReachability();
        }

        private void SetupNetworkReachability()
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
        }

        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            var networkLevel = NetworkInformation.GetInternetConnectionProfile();

            if (IsConnected)
            {
                await ApiInstance.TakeNext();
            }
        }

        private static bool IsConnected
        {
            get
            {
                var profiles = NetworkInformation.GetConnectionProfiles();
                var internetProfile = NetworkInformation.GetInternetConnectionProfile();
                return profiles.Any(s => s.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
                   || (internetProfile != null
                           && internetProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
            }
        }

        
        private string GetPayload(string rawUri)
        {
            Logger.Debugf("Getting payload from uri: {0}", rawUri);

            var decoder = new WwwFormUrlDecoder("?q=" + rawUri.Substring(UriHelper.UriPrefix.Length));
            var payload = decoder.GetFirstValueByName("q");

            Logger.Debugf("Got payload: \"{0}\"", payload);

            return payload;
        }

        private string GetPayload(Uri uri)
        {
            if (uri == null)
            {
                return "";
            }

            if (!uri.isPushBoxUri())
            {
                return "";
            }

            var rawUri = uri.ToString();

            return GetPayload(rawUri);
        }

        private void SetupMPNSChannel()
        {
            Logger.Debug("Setting up...");
            if (IsSetup)
            {
                return;
            }
            else
            {
                IsSetup = true;
            }

            HttpNotificationChannel pushChannel;

            string channelName = "PushBoxChannel";

            pushChannel = HttpNotificationChannel.Find(channelName);

            var pushChannelWasNull = false;
            if (pushChannel == null)
            {
                // TOOD: serviceName for authenticated push?
                // https://blogs.windows.com/buildingapps/2013/06/06/no-quota-push-notifications-using-a-root-certificate-authority/
                pushChannel = new HttpNotificationChannel(channelName);
                pushChannelWasNull = true;
            }

            pushChannel.ChannelUriUpdated += PushChannel_ChannelUriUpdated;
            pushChannel.ErrorOccurred += PushChannel_ErrorOccurred;
            pushChannel.HttpNotificationReceived += PushChannel_HttpNotificationReceived;
            pushChannel.ShellToastNotificationReceived += PushChannel_ShellToastNotificationReceived;

            if (pushChannelWasNull)
            {
                pushChannel.Open();
            }

            pushChannel.BindToShellToast();

            Logger.Debug("Did setup MPNS channel.");
        }

        #region HttpNotificationChannel handlers

        private void PushChannel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e)
        {
            Logger.Debugf("Got http notification: \"{0}\" with body: \"{1}\"", e.Notification.ToString(), e.Notification.ReadBody());
        }

        private void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            if (OnPushError != null)
            {
                OnPushError(sender, e);
            }
        }

        private void PushChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            Logger.Debugf("Got shell toast notification: {0}", e.ToString());

            var helper = new MpnsHelper(new Logger("MpnsHelper", this.Logger.Level));
            var data = helper.NotificationEventToToastData(e);
            Logger.Debugf("Handling toast data: {0}", data);
            var payload = "";
            PushBoxMessage? message = null;
            try
            {
                payload = GetPayload(data.Param);
                message = MessageFromPayload(payload);
            }
            catch (FormatException formatError)
            {
                Logger.Debug(formatError.Message);
            }

            if (message.HasValue)
            {
                TriggerPush(message.Value);
            }
        }

        private static PushBoxMessage? MessageFromPayload(string payload)
        {
            var maybeMiniMessage = SerializationHelper.DataContractDeserializeStruct<MiniPushBoxMessage>(payload);

            if (maybeMiniMessage != null)
            {
                var miniMessage = maybeMiniMessage.Value;
                var message = new PushBoxMessage(miniMessage.Title, miniMessage.Message, miniMessage.Payload);
                message.Id = miniMessage.Id;
                message.ExpirationDate = miniMessage.ExpirationDate;
                message.Badge = miniMessage.Badge;

                return message;
            } else
            {
                return null;
            }
        }

        private void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            Logger.Debugf("Got uri updated: {0}", e.ChannelUri);
            ApiInstance.Token = e.ChannelUri.ToString();
        }

        #endregion

        private static Dictionary<string, object> NewParameter(string key, object value)
        {
            return new Dictionary<string, object>() { { key, value } };
        }

        #region api methods

        /// <summary>
        /// Mark message as read on backend
        /// </summary>
        /// <param name="message"></param>
        public async void SetMessageRead(PushBoxMessage message)
        {
            var parameters = new Dictionary<string, object>()
            {
                { Constants.JSONKeyPushId, message.Id },
                { Constants.JSONKeyPushReadTime, DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:SS") },
            };

            await ApiInstance.QueueRequest(Constants.MethodPushRead, parameters);
        }

        /// <summary>
        /// Request list of messages in inbox. Listen for OnMessagesReceived for getting messages.
        /// </summary>
        public async void GetMessages()
        {
            await ApiInstance.QueueRequest(Constants.MethodInbox);
        }

        /// <summary>
        /// Set age for profile.
        /// </summary>
        /// <param name="age"></param>
        public async void SetAge(int age)
        {
            await ApiInstance.QueueRequest(Constants.MethodSetAge, NewParameter(Constants.JSONKeyAge, age));
        }

        /// <summary>
        /// Set birthday for profile.
        /// </summary>
        /// <param name="birthday"></param>
        /// <returns></returns>
        public async Task SetBirthday(DateTime birthday)
        {
            await ApiInstance.QueueRequest(
                Constants.MethodSetBirthday,
                NewParameter(Constants.JSONKeyBirthday, birthday.ToString("yyyy/MM/dd"))
            );
        }

        /// <summary>
        /// Log event for profile.
        /// </summary>
        /// <param name="eventStr"></param>
        public async void LogEvent(string eventStr)
        {
            await ApiInstance.QueueRequest(Constants.MethodLogEvent, NewParameter(Constants.JSONKeyEvent, eventStr));
        }

        /// <summary>
        /// Log location for profile.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longtitude"></param>
        public async void LogLocation(double latitude, double longtitude)
        {
            var parameters = new Dictionary<string, object>()
                {
                    { Constants.JSONKeyLocationLatitude, latitude },
                    { Constants.JSONKeyLocationLongitude, longtitude },
                };

            await ApiInstance.QueueRequest(Constants.MethodLogLocation, parameters);
        }

        /// <summary>
        /// Set gender for profile.
        /// </summary>
        /// <param name="gender"></param>
        public async void SetGender(Gender gender)
        {
            await ApiInstance.QueueRequest(Constants.MethodSetGender, NewParameter(Constants.JSONKeyGender, (int)gender));
        }

        /// <summary>
        /// Add channel subscriptions for profile.
        /// </summary>
        /// <param name="channels"></param>
        public async void SetChannels(List<string> channels)
        {
            await ApiInstance.QueueChannelRequest(Constants.MethodSetChannels, channels);
        }

        #endregion

        private static int GetUnixTimestamp()
        {
            var epoch = (new DateTime(1970, 1, 1)).ToUniversalTime();
            return (Int32)DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }

        private class Api
        {
            private string ApiKey { get; set; }
            private string ApiSecret { get; set; }
            private ILogger Logger { get; set; }

            private bool IsWorking { get; set; }
            private bool IsReady
            {
                get
                {
                    return Token != null && Token != "";
                }
            }
            private bool IsTokenSent { get; set; }

            private string _token;
            internal string Token
            {
                get { return _token; }
                set
                {
                    _token = value;
                    Logger.Debugf("Did set token {0}", _token);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    TakeNext();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }

            private string _uid;
            private string Uid
            {
                get
                {
                    if (_uid == "" || _uid == null)
                    {
                        var localSettings = new LocalSettingsHelper();
                        _uid = localSettings.TryGetValueWithDefault<string>(Constants.LocalSettingsKeyUid, "");
                    }

                    return _uid;
                }
                set
                {
                    var localSettings = new LocalSettingsHelper();
                    localSettings.AddOrUpdateValue(Constants.LocalSettingsKeyUid, value);
                    _uid = value;
                }
            }

            private Queue<RequestQueueItem> _requestQueue;
            private Queue<RequestQueueItem> RequestQueue
            {
                get
                {
                    if (_requestQueue == null)
                    {
                        var localSettings = new LocalSettingsHelper();
                        var requestQueueListSerialized = localSettings.TryGetValueWithDefault<string>(Constants.LocalSettingsKeyQueue, null);

                        if (requestQueueListSerialized != null && requestQueueListSerialized != "")
                        {
                            Logger.Debugf("Deserializing: {0}", requestQueueListSerialized);
                            var savedRequestQueue = SerializationHelper.DataContractDeserializeClass<List<RequestQueueItem>>(requestQueueListSerialized);

                            Logger.Debugf("Got {0}", savedRequestQueue);
                            if (savedRequestQueue != null)
                            {
                                _requestQueue = new Queue<RequestQueueItem>(savedRequestQueue);
                            }
                        }
                    }

                    if (_requestQueue == null)
                    {
                        _requestQueue = new Queue<RequestQueueItem>();
                    }

                    return _requestQueue;
                }
                set
                {
                    var localSettings = new LocalSettingsHelper();
                    Logger.Debugf("Serializing queue: {0}", value);
                    var serialized = SerializationHelper.DataContractSerializeObject(value.ToList());
                    Logger.Debugf("Serialized: {0}", serialized);
                    localSettings.AddOrUpdateValue(Constants.LocalSettingsKeyQueue, serialized);
                    _requestQueue = value;
                }
            }

            [DataContract]
            internal class RequestQueueItem
            {
                [DataMember]
                public string ApiMethod { get; set; }

                [DataMember]
                public Dictionary<string, object> Body { get; set; }

                [DataMember]
                public List<String> Channels { get; set; }

                public RequestQueueItem(string apiMethod, Dictionary<string, object> body)
                {
                    ApiMethod = apiMethod;
                    Body = body;
                }
            }

            internal Api(string apiKey, string apiSecret, ILogger logger)
            {
                this.ApiKey = apiKey;
                this.ApiSecret = apiSecret;
                this.Logger = logger;
            }

            private string Hmac(string apiKey, string secret, int unixTimestamp)
            {
                var format = apiKey + ":" + unixTimestamp.ToString();

                Logger.Debugf("Hashing: {0}", format);

                byte[] key = Encoding.UTF8.GetBytes(secret);
                byte[] data = Encoding.UTF8.GetBytes(format);
                var hmac = new HMACSHA1(key);

                var resultData = hmac.ComputeHash(data);
                return resultData.ToHexString();
            }

            internal async void LogPushInteracted(int pushId)
            {
                var parameters = new Dictionary<string, object>()
                {
                    { Constants.JSONKeyPushReadTime,  DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) },
                    { Constants.JSONKeyPushId,  pushId },
                };

                await QueueRequest(Constants.MethodPushInteracted, parameters);
            }

            /// <summary>
            /// Perform a request to the api.
            /// </summary>
            /// <typeparam name="T">Response type for deserialization</typeparam>
            /// <param name="apiMethod"></param>
            /// <param name="parameters">Merge and overwrite default post body keys.</param>
            /// <returns></returns>
            internal async Task QueueRequest(string apiMethod, Dictionary<string, object> parameters = null)
            {
                Dictionary<string, object> postBodyData = GetRequestBody();

                if (parameters != null)
                {
                    parameters.ToList().ForEach((i) => { postBodyData[i.Key] = i.Value; });
                }

                await Queue(new RequestQueueItem(apiMethod, postBodyData));
            }

            internal async Task QueueChannelRequest(string apiMethod, List<string> channels)
            {
                Dictionary<string, object> postBodyData = GetRequestBody();
                var requestItem = new RequestQueueItem(apiMethod, postBodyData);
                requestItem.Channels = channels;
                await Queue(requestItem);
            }

            private async Task Queue(RequestQueueItem requestItem)
            {
                var queue = RequestQueue;
                queue.Enqueue(requestItem);
                RequestQueue = queue;

                await TakeNext();
            }

            private Dictionary<string, object> GetRequestBody()
            {
                var unixTimestamp = GetUnixTimestamp();
                var postBodyData = new Dictionary<String, object>
                {
                    { Constants.JSONKeyApiKey, ApiKey },
                    { Constants.JSONKeyPlatform,  Constants.Platform },
                    { Constants.JSONKeyTS, unixTimestamp },
                    { Constants.JSONKeyHMAC, Hmac(ApiKey, ApiSecret, unixTimestamp) },
                };

                return postBodyData;
            }

            internal async Task TakeNext()
            {
                var handled = false;
                Logger.Debug("Take next...");
                if (!IsReady)
                {
                    Logger.Debug("Not ready");
                    return;
                }
                if (IsWorking)
                {
                    Logger.Debug("Busy");
                    return;
                }
                IsWorking = true;

                if (!IsTokenSent)
                {
                    Logger.Debug("Token was not sent");
                    var requestBody = GetRequestBody();
                    requestBody[Constants.JSONKeyToken] = Token;
                    Logger.Debug("Sending token first");
                    var setTokenResponse = await ExecuteRequest(new RequestQueueItem(
                        Constants.MethodSetToken,
                        requestBody
                    ));
                    handled = HandleResponse(Constants.MethodSetToken, setTokenResponse);
                }
                else
                {
                    var queue = RequestQueue;
                    Logger.Debugf("Working on queue: {0}", queue);
                    if (queue.Count != 0)
                    {
                        var nextRequest = RequestQueue.Peek();
                        var response = await ExecuteRequest(nextRequest);
                        handled = HandleResponse(nextRequest.ApiMethod, response);
                    }
                    else
                    {
                        IsWorking = false;
                        return;
                    }
                }

                IsWorking = false;
                Logger.Debug("Taking next...");
                if (handled)
                {
                    await TakeNext();
                }
            }

            private bool HandleResponse(string apiMethod, IRestResponse<Response> response)
            {
                Logger.Debugf("Handling response(method={0}): {1}", apiMethod, response.Data);
                Logger.Debug(response.Content);
                if (response.Data.Success)
                {
                    if (apiMethod == Constants.MethodSetToken)
                    {
                        Uid = response.Data.Uid;
                        IsTokenSent = true;
                    }
                    else
                    {
                        Logger.Debug("Pop the request!");
                        PopRequest();
                    }

                    Logger.Debug("Success event!");
                    var eventArgs = new OnRequestSuccessEventArgs(apiMethod, response.Data.Message ?? "");
                    PushBoxSDK.Instance.RequestSuccess(eventArgs);

                    if (apiMethod == Constants.MethodInbox)
                    {
                        PushBoxSDK.Instance.MessagesReceived(response.Data.Messages);
                    }

                    Logger.Debug("Did send request success event");
                    return true;
                }
                else
                {
                    var message = response.Data.Message ?? "";

                    OnRequestErrorEventArgs eventArgs;
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        eventArgs = new OnRequestErrorEventArgs(OnRequestErrorEventArgs.Type.AuthorizationError, apiMethod, message);
                    }
                    else
                    {
                        eventArgs = new OnRequestErrorEventArgs(OnRequestErrorEventArgs.Type.ApiError, apiMethod, message);
                    }

                    Logger.Debug("Error event!");
                    PushBoxSDK.Instance.RequestError(eventArgs);
                    return false;
                }
            }

            private void PopRequest()
            {
                var queue = RequestQueue;
                queue.Dequeue();
                RequestQueue = queue;
            }

            private async Task<IRestResponse<Response>> ExecuteRequest(RequestQueueItem requestData)
            {
                var client = new RestClient(Constants.ApiUrl);
                var request = new RestRequest(requestData.ApiMethod);
                request.Method = Method.POST;
                request.RequestFormat = DataFormat.Json;

                var body = requestData.Body;
                if (Uid != null && Uid != "")
                {
                    Logger.Debugf("Requesting with uid: {0}", Uid);
                    body[Constants.JSONKeyUid] = Uid;
                }

                if (requestData.Channels != null)
                {
                    body[Constants.JSONKeyChannels] = requestData.Channels;
                }
                request.AddJsonBody(body);

                if (Logger.Level <= LogLevel.Debug)
                {
                    Logger.Debugf("Requesting({0}): {1}", requestData.ApiMethod, request.ToString());
                }

                return await client.ExecuteTaskAsync<Response>(request);
            }
        }
    }
}
