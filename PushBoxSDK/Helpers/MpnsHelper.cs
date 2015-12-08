using HouseOfCode.Helpers;
using Microsoft.Phone.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseOfCode.Helpers
{
    public class MpnsHelper
    {
        public ILogger Logger { get; private set; }

        public MpnsHelper(ILogger logger)
        {
            this.Logger = logger;
        }

        public struct ToastData
        {
            public string Text1;
            public string Text2;
            public string Param;
        }

        public ToastData NotificationEventToToastData(NotificationEventArgs e)
        {
            var data = new ToastData();

            foreach (string key in e.Collection.Keys)
            {
                var value = e.Collection[key];
                Logger.Debugf("Handling notification event key {0} => {1}", key, value);
                if (key.EqualsCaseInsensitive("wp:text1"))
                {
                    data.Text1 = value;
                }
                else if (key.EqualsCaseInsensitive("wp:text2"))
                {
                    data.Text2 = value;
                }
                else if (key.EqualsCaseInsensitive("wp:param"))
                {
                    data.Param = value;
                }
                else
                {
                    Logger.Warnf("Unhandled notification event collection item: \"{0}\": \"{1}\"",
                        key, value);
                }
            }

            return data;
        }
    }
}
