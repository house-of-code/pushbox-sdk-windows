using Windows.ApplicationModel;
using Windows.Storage;
using HouseOfCode.PushBoxSDK.Api;
using static System.String;

namespace HouseOfCode.PushBoxSDK.Helpers
{
    internal static class PackageHelper
    {
        public static bool IsUpdated(Package package)
        {
            var currentVersion = Format("{0}.{1}.{2}.{3}",
                Package.Current.Id.Version.Build,
                Package.Current.Id.Version.Major,
                Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Revision
                );

            var localSettings = ApplicationData.Current.LocalSettings;

            var oldVersion = localSettings.Values[Constants.LocalSettingsKeyAppVersion] as string;

            if (IsNullOrEmpty(oldVersion))
            {
                localSettings.Values[Constants.LocalSettingsKeyAppVersion] = currentVersion;
                return false;
            }

            var isUpdated = currentVersion != oldVersion;

            if (isUpdated)
            {
                localSettings.Values[Constants.LocalSettingsKeyAppVersion] = currentVersion;
            }

            return isUpdated;
        }
    }
}