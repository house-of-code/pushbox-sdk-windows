using HouseOfCode.PushBoxSDK.Api;
using HouseOfCode.PushBoxSDK.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.Networking.PushNotifications;
using HousePushBoxSDK.Helpers;

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

        private Api ApiInstance { get; set; }
        public readonly ILogger Logger;
        private bool IsSetup { get; set; } = false;

        public event EventHandler<OnPushEventArgs> OnPush;
        public event EventHandler<OnPushErrorEventArgs> OnPushError;
        public event EventHandler<OnRequestErrorEventArgs> OnRequestError;
        public event EventHandler<OnRequestSuccessEventArgs> OnRequestSuccess;
        public event EventHandler<OnMessagesReceivedEventArgs> OnMessagesReceived;

        internal void RequestError(OnRequestErrorEventArgs eventArgs)
        {
            OnRequestError?.Invoke(this, eventArgs);
        }

        internal void RequestSuccess(OnRequestSuccessEventArgs eventArgs)
        {
            OnRequestSuccess?.Invoke(this, eventArgs);
        }

        internal void MessagesReceived(List<PushBoxMessage> messages)
        {
            OnMessagesReceived?.Invoke(this, new OnMessagesReceivedEventArgs(messages));
        }

        private void TriggerPush(PushBoxMessage message)
        {
            var eventArgs = new OnPushEventArgs(message);
            Debug.Assert(OnPush != null, "OnPush != null");
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
        /// Api has been setup, start registering channel/setting push token
        /// </summary>
        private void Start()
        {
            if (IsSetup)
            {
                return;
            }
            IsSetup = true;

            SetupWnsChannel();
            // Subscribe to network connectivity events
            SetupNetworkReachability();
        }

        private async void SetupWnsChannel()
        {
            try
            {
                var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                channel.PushNotificationReceived += ChannelOnPushNotificationReceived;
                ApiInstance.Token = channel.Uri;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message);
            }
        }

        private void ChannelOnPushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            Logger.Debug("Got push!");
            if (args.NotificationType != PushNotificationType.Raw)
            {
                Logger.Warnf("Got unsupported notification type: {0}", args.NotificationType);
                return;
            }

            var serialized = args?.RawNotification?.Content ?? "";
            try
            {
                Logger.Debugf("Raw push content: {0}", serialized);
                var message = SimpleJson.DeserializeObject<PushBoxMessage>(serialized, SimpleJson.DataContractJsonSerializerStrategy);
                if (message != null)
                {
                    TriggerPush(message);
                }
            }
            catch (SerializationException se)
            {
                Logger.Warn($"Could not deserialize pushbox message: [{se.Message}], from: \"{serialized}\"");
                OnPushError?.Invoke(this, new OnPushErrorEventArgs(se));
            }
        }

        private void SetupNetworkReachability()
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
        }

        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            try
            {
                if (!IsConnected) return;

                await ApiInstance.TakeNext();
            }
            catch (Exception e)
            {
                Logger.Warn(e.Message);
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
            try
            {
                var parameters = new Dictionary<string, object>()
                {
                    {Constants.JSONKeyPushId, message.Id},
                    {Constants.JSONKeyPushReadTime, DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:SS")},
                };

                await ApiInstance.QueueRequest(Constants.MethodPushRead, parameters);
            }
            catch (Exception e)
            {
                Logger.Warn(e.Message);
            }
        }

        /// <summary>
        /// Request list of messages in inbox. Listen for OnMessagesReceived for getting messages.
        /// </summary>
        public async void GetMessages()
        {
            try
            {
                await ApiInstance.QueueRequest(Constants.MethodInbox);
            }
            catch (Exception e)
            {
                Logger.Warn(e.Message);
            }
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
            return (int)DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }

        private class Api
        {
            private string ApiKey { get; set; }
            private string ApiSecret { get; set; }
            private ILogger Logger { get; set; }

            private bool IsWorking { get; set; }
            private bool IsReady => !string.IsNullOrEmpty(Token);
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
                    if (string.IsNullOrEmpty(_uid))
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

                        if (!string.IsNullOrEmpty(requestQueueListSerialized))
                        {
                            Logger.Debugf("Deserializing: {0}", requestQueueListSerialized);
                            try
                            {
                                var savedRequestQueue = SimpleJson.DeserializeObject<List<RequestQueueItem>>(requestQueueListSerialized);

                                Logger.Debugf("Got {0}", savedRequestQueue);
                                if (savedRequestQueue != null)
                                {
                                    _requestQueue = new Queue<RequestQueueItem>(savedRequestQueue);
                                }
                            }
                            catch (NullReferenceException) { }
                            catch (Exception e)
                            {
                                Logger.Warn(e.Message);
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
                    List<RequestQueueItem> objectToSerialize = value.ToList();
                    var serialized = SimpleJson.SerializeObject(objectToSerialize);
                    Logger.Debugf("Serialized: {0}", serialized);
                    localSettings.AddOrUpdateValue(Constants.LocalSettingsKeyQueue, serialized);
                    _requestQueue = value;
                }
            }

            [DataContract]
            public class RequestQueueItem
            {
                [DataMember]
                public string ApiMethod { get; }

                [DataMember]
                public Dictionary<string, object> Body { get; }

                [DataMember]
                public List<string> Channels { get; set; }

                public RequestQueueItem(string apiMethod, Dictionary<string, object> body)
                {
                    ApiMethod = apiMethod;
                    Body = body;
                    Channels = new List<string>();
                }
            }

            internal Api(string apiKey, string apiSecret, ILogger logger)
            {
                this.ApiKey = apiKey;
                this.ApiSecret = apiSecret;
                this.Logger = logger;
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
                var postBodyData = GetRequestBody();

                if (parameters != null)
                {
                    foreach (var pair in parameters)
                    {
                        postBodyData[pair.Key] = pair.Value;
                    }
                }

                await Queue(new RequestQueueItem(apiMethod, postBodyData));
            }

            internal async Task QueueChannelRequest(string apiMethod, List<string> channels)
            {
                try
                {
                    var postBodyData = GetRequestBody();
                    var requestItem = new RequestQueueItem(apiMethod, postBodyData) {Channels = channels};
                    await Queue(requestItem);
                }
                catch (Exception e)
                {
                    Logger.Warnf("Exception on queuing request: {0}", e.Message);

                }
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
                    { Constants.JSONKeyHMAC, CryptographyHelper.CreateHash(ApiKey, ApiSecret, unixTimestamp) },
                };

                return postBodyData;
            }

            internal async Task TakeNext()
            {
                bool handled;
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
                    Logger.Debugf("Working on queue: [{0}]{1}", queue.Count, queue);
                    if (queue.Count != 0)
                    {
                        var nextRequest = RequestQueue.Peek();
                        Logger.Debug($"Peeked: {nextRequest.ApiMethod}");
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

                if (response.Data == null)
                {
                    Logger.Debugf("Got unparseable response.");
                    return false;
                }

                var responseData = response.Data;
                if (responseData.Success)
                {
                    if (apiMethod == Constants.MethodSetToken)
                    {
                        Uid = responseData.Uid;
                        IsTokenSent = true;
                    }
                    else
                    {
                        Logger.Debug("Pop the request!");
                        PopRequest();
                    }

                    Logger.Debug("Success event!");
                    var eventArgs = new OnRequestSuccessEventArgs(apiMethod, responseData.Message ?? "");
                    Instance.RequestSuccess(eventArgs);

                    if (apiMethod == Constants.MethodInbox)
                    {
                        Instance.MessagesReceived(responseData.Messages);
                    }

                    Logger.Debug("Did send request success event");
                    return true;
                }
                else
                {
                    var message = responseData.Message ?? "";

                    OnRequestErrorEventArgs eventArgs;
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        eventArgs = new OnRequestErrorEventArgs(OnRequestErrorEventArgs.Type.AuthorizationError,
                            apiMethod, message);
                    }
                    else
                    {
                        eventArgs = new OnRequestErrorEventArgs(OnRequestErrorEventArgs.Type.ApiError, apiMethod,
                            message);
                    }

                    Logger.Debug("Error event!");
                    Instance.RequestError(eventArgs);
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
                Logger.Debugf("Executing... {0}", requestData.ApiMethod);
                var methodUri = new Uri(new Uri(Constants.ApiUrl), requestData.ApiMethod);
                var handler = new HttpClientHandler {UseProxy = true};
                var httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                var body = requestData.Body;
                if (!string.IsNullOrEmpty(Uid))
                {
                    Logger.Debugf("Requesting with uid: {0}", Uid);
                    body[Constants.JSONKeyUid] = Uid;
                }

                if (requestData.ApiMethod == Constants.MethodSetChannels)
                {
                    var channels = requestData.Channels;
                    if (channels != null && channels.Count > 0)
                    {
                        body[Constants.JSONKeyChannels] = requestData.Channels;
                    }
                }

                var serializedJsonBody = SimpleJson.SerializeObject(body);
                
                Logger.Debugf("Sending body: {0}", serializedJsonBody);

                try
                {
                    var httpResponse =
                        await
                            httpClient.PostAsync(methodUri,
                                new StringContent(serializedJsonBody, Encoding.UTF8, "application/json"));

                    var responseContent = await httpResponse.Content.ReadAsStringAsync();

                    var response = SimpleJson.DeserializeObject<Response>(responseContent);
                    
                    Logger.Debugf("Got response({0}): {1}", httpResponse.StatusCode, responseContent);

                    return new RestResponse(response, httpResponse.StatusCode);
                }
                catch (Exception ex)
                { 
                    Logger.Warn(ex.Message);
                    Instance.RequestError(new OnRequestErrorEventArgs(ex));
                }
                finally
                {
                    httpClient.Dispose();
                }

                return null;
            }
        }
    }

    internal interface IRestResponse<out T>
    {
        T Data { get; }
        HttpStatusCode StatusCode { get; }
    }

    internal struct RestResponse : IRestResponse<Response>
    {
        public Response Data { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }

        public RestResponse(Response data, HttpStatusCode statusCode)
        {
            Data = data;
            StatusCode = statusCode;
        }
    }
}
