using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace HouseOfCode.PushBoxSDK.Helpers
{
    class LocalSettingsHelper
    {
        public LocalSettingsHelper()
        {
            try
            {
                localSettings = ApplicationData.Current.LocalSettings;
            }
            catch (Exception)
            {
            }
        }
        ApplicationDataContainer localSettings;
        public TValue TryGetValueWithDefault<TValue>(string key, TValue defaultvalue)
        {
            TValue value;

            if (localSettings.Values.ContainsKey(key))
            {
                value = (TValue)localSettings.Values[key];
            }
            else
            {
                value = defaultvalue;
            }

            return value;
        }

        public bool AddOrUpdateValue(string key, object value)
        {
            bool valueChanged = false;

            if (localSettings.Values.ContainsKey(key))
            {
                if (localSettings.Values[key] != value)
                {
                    localSettings.Values[key] = value;
                    valueChanged = true;
                }
            }
            else
            {
                localSettings.Values.Add(key, value);
                valueChanged = true;
            }

            return valueChanged;
        }
    }
}
