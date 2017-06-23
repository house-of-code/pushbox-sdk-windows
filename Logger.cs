using System;

namespace HouseOfCode.PushBoxSDK
{
    public class Logger : ILogger
    {
        private string tag = "";

        const LogLevel DefaultLogLevel = LogLevel.Error;

        LogLevel _level;
        LogLevel ILogger.Level
        {
            get
            {
                return _level;
            }

            set
            {
                _level = value;
            }
        }

        private void Logf(string level, string message, params object[] args)
        {
            Log(level, String.Format(message, args));
        }

        public void Log(string level, string message)
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format("[{0}] [{1}] ", tag, level) +
                message
            );
        }

        public void Debug(string message)
        {
            if (_level <= LogLevel.Debug)
            {
                Log("D", message);
            }
        }

        public void Debugf(string message, params object[] args)
        {
            if (_level <= LogLevel.Debug)
            {
                Logf("D", message, args);
            }
        }

        public void Warn(string message)
        {
            if (_level <= LogLevel.Warn)
            {
                Logf("W", message);
            }
        }

        public void Warnf(string message, params object[] args)
        {
            if (_level <= LogLevel.Warn)
            {
                Logf("W", message, args);
            }
        }

        public void Warn(Exception e, string message)
        {
            Warnf("{0}: {1}", message, e.Message);
        }

        public Logger(string tag, LogLevel logLevel = DefaultLogLevel)
        {
            this.tag = tag;
            this._level = logLevel;
        }

        public static ILogger WithType<T>(LogLevel logLevel = DefaultLogLevel)
        {
            return new Logger(typeof (T).FullName, logLevel);
        }
    }
}
