using System;

namespace NativeClient
{
    class Timestamps
    {
        /// <summary>
        /// Computes Unix timestamp for the current moment.
        /// </summary>
        /// <value>The unix timestamp.</value>
        public static long UnixTimestamp
        {
            get
            {
                TimeSpan span = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
                return (long)span.TotalSeconds;
            }
        }

        /// <summary>
        /// Returns a number of milliseconds that has passed since 04.12.2013 19:50:00. This value will be relatively
        /// small and may be precisely stored in a double.
        /// </summary>
        /// <value>Number of milliseconds since 04.12.2013 19:50:00.</value>
        public static double DoubleMilliseconds
        {
            get
            {
                TimeSpan span = (DateTime.Now - DateTime.Today);
                return span.TotalMilliseconds;
            }
        }
    }
}

