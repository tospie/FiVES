using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NativeClient
{
    class Statistics
    {
        public static Statistics Instance = new Statistics();

        public Statistics()
        {
        }

        public void ReportMessageSent(List<object> message, int serializedMessageSize)
        {
            string logMessage = "ClientSentMessage Class=" + message[0].ToString();
            if (message[0].Equals("call"))
                logMessage += " FuncName=" + message[2];
            logMessage += " Size=" + serializedMessageSize;
            logger.Debug(logMessage);
        }

        public void ReportMessageReceived(List<JToken> message, int serializedMessageSize)
        {
            string logMessage = "ClientReceivedMessage Class=" + message[0].ToString();
            TimeSpan logTimeSpan = new TimeSpan(DateTime.Now.Ticks);
            double timeMS = logTimeSpan.TotalMilliseconds;

            if (message[0].ToString().Equals("call-reply"))
            {
                logMessage += " FuncName=" + message[2].ToString();
                logMessage += " MessageID=" + message[1];
                logMessage += " ReceivedTime=" + timeMS;
                logMessage += " Size=" + serializedMessageSize;
                logger.Debug(logMessage);
            }
        }

        public void ReportMessageHandlingFinished(List<JToken> message)
        {
            string logMessage = "ClientFinishedMessageHandling ";
            TimeSpan logTimeSpan = new TimeSpan(DateTime.Now.Ticks);
            double timeMS = logTimeSpan.TotalMilliseconds;

            if (message[0].ToString().Equals("call-reply"))
            {
                logMessage += " MessageID=" + message[1];
                logMessage += " FinishedTime=" + timeMS;
                logger.Debug(logMessage);
            }
        }

        public void ReportObjectUpdate(CallRequest request)
        {
            // Go over all updates and extract updates to attribute position.x, which contains the number of ticks
            // in DateTime.Now when this update was sent. We can then detect the update delay.
            List<JToken> updates = request.Args[0].ToObject<List<JToken>>();
            foreach (JToken update in updates)
            {
                if (update["componentName"].ToString() == "position")
                {
                    var attributeName = update["attributeName"].ToString();
                    var attributeValue = update["value"].ToObject<double>();
                    if (attributeName == "x")
                    {
                        double startTimestampMs = attributeValue;
                        double endTimestampMs = Timestamps.DoubleMilliseconds;
                        double delay = endTimestampMs - startTimestampMs;
                            logger.Info("UpdateDelayMs=" + (delay) +
                                " StartTimeStamp=" + startTimestampMs +
                                        " EndTimeStamp=" + endTimestampMs);
                    }
                    else if (attributeName == "y")
                    {
                        double updateDelay = attributeValue;
                        if (updateDelay == 1e-06) updateDelay = 0.0;
                        // hack to filter out update delays of server invoked updates, e.g. from motion
                        if (updateDelay >= 0 && updateDelay < 10000)
                            logger.Info("DelayToAttributeUpdate=" + updateDelay);
                    }
                    else if (attributeName == "z")
                    {
                        double queueProcessingTime = attributeValue;
                        // hack to filter out update delays of server invoked updates, e.g. from motion
                        if (queueProcessingTime >= 0 && queueProcessingTime < 10000)
                            logger.Info("QueueProcessingTime=" + queueProcessingTime);
                    }
                }

                if (update["componentName"].ToString() == "orientation")
                {
                    var attributeName = update["attributeName"].ToString();
                    var attributeValue = update["value"].ToObject<double>();
                    if (attributeName == "x")
                    {
                        logger.Info("Queue Length: " + attributeValue);
                    }
                }
            }
        }

        static Logger logger = LogManager.GetCurrentClassLogger();
    }
}
