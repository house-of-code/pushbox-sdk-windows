using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;
using HouseOfCode.PushBoxSDK.Helpers;

namespace HouseOfCode.PushBoxSDK
{
    /// <summary>
    ///     Helper for handling triggered background task by PushBox push
    /// </summary>
    public class PushBoxSDKBackgroundTask
    {
        public ILogger Logger { get; set; }

        /// <summary>
        ///     Handler for triggered background task. Use in Run function in background task implementation.
        /// </summary>
        /// <param name="taskInstance"></param>
        /// <returns>Parsed PushBox message or null</returns>
        public PushBoxMessage HandleBackgroundTaskTriggered(IBackgroundTaskInstance taskInstance)
        {
            var notification = taskInstance.TriggerDetails as RawNotification;

            if (notification == null)
            {
                Logger.Warn("Did not get RawNotification in background task");
                return null;
            }

            try
            {
                return PushBoxSDK.ParsePushBoxMessage(notification.Content);
            }
            catch (SerializationException se)
            {
                Logger.Warn(se.Message);
            }
            return null;
        }

        /// <summary>
        ///     Register background task for raw notifications.
        /// </summary>
        /// <param name="package">Application package</param>
        /// <param name="taskName"></param>
        /// <param name="taskEntryPoint"></param>
        public async void RegisterBackgroundTask(Package package, string taskName, string taskEntryPoint)
        {
            try
            {
                await RegisterBackgroundTaskAsync(package, taskName, taskEntryPoint);
            }
            catch (Exception e)
            {
                Logger.Warn(e.Message);
            }
        }

        /// <summary>
        ///     Register background task for raw notifications.
        /// </summary>
        /// <param name="package">Application package</param>
        /// <param name="taskName"></param>
        /// <param name="taskEntryPoint"></param>
        public void RegisterBackgroundTask<T>(Package package, string taskName)
        {
            RegisterBackgroundTask(package, taskName, typeof (T).FullName);
        }

        /// <summary>
        ///     Register background task for raw notifications.
        /// </summary>
        /// <param name="package">Application package</param>
        /// <param name="taskName"></param>
        /// <param name="taskEntryPoint"></param>
        public async Task RegisterBackgroundTaskAsync(Package package, string taskName, string taskEntryPoint)
        {
            if (PackageHelper.IsUpdated(package))
            {
                BackgroundExecutionManager.RemoveAccess();
            }

            await BackgroundExecutionManager.RequestAccessAsync();

            Logger.Debugf("Registering task: name={0}, entryPoint={1}", taskName, taskEntryPoint);
            var allTasks = BackgroundTaskRegistration.AllTasks;
            var taskRegistered = allTasks.Any(task => task.Value.Name == taskName);

            if (taskRegistered)
            {
                return;
            }

            var builder = new BackgroundTaskBuilder
            {
                Name = taskName,
                TaskEntryPoint = taskEntryPoint
            };

            builder.SetTrigger(new PushNotificationTrigger());

            builder.Register();
        }

        #region singleton

        private static readonly Lazy<PushBoxSDKBackgroundTask> _instance =
            new Lazy<PushBoxSDKBackgroundTask>(() => new PushBoxSDKBackgroundTask());

        private PushBoxSDKBackgroundTask()
        {
            Logger = new Logger(typeof (PushBoxSDKBackgroundTask).Name);
        }

        public static PushBoxSDKBackgroundTask Instance => _instance.Value;

        #endregion
    }
}