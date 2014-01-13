#!/bin/bash

if [ $# -gt 0 ]; then
    numClients=$1
else
    numClients=10
fi

if [ $# -gt 1 ]; then
    duration=$2
else
    duration=60
fi

if [ $# -gt 2 ]; then
    start_delay=$3
else
    start_delay=0
fi

# Collect logs for 60 seconds
rm Logs -fr
mkdir -p Logs
killall screen
echo "Starting $numClients clients..."
for i in $(seq -f "%05g" 1 $numClients); do
  screen -S client$i -d -m ./NativeClient.exe --logfile=Logs/Client$i.log
  sleep $start_delay
done
echo "Waiting for $duration seconds..."
echo "Clean test start: " $(date +%T.%N)
sleep $duration
echo "Clean test end: " $(date +%T.%N)
echo "Stopping clients..."
killall screen
echo "Experiment complete"

# Merging logs
echo "Merging logs..."
#tar -czf Clients.log Logs
cat Logs/*.log | sort > Clients.log
cat Clients.log | grep UpdateDelayMs=[0-9] | sed 's/.*UpdateDelayMs=\([0-9\.]*\).*/\1/' > delay.dat
cat Clients.log | grep DelayToAttributeUpdate=[0-9] | sed 's/.*DelayToAttributeUpdate=\([0-9\.]*\)/\1/' > attributeDelay.dat
cat Clients.log | grep QueueProcessingTime | sed 's/.*QueueProcessingTime=\([0-9]*\)*/\1/' > queueProcessing.dat
cat Clients.log | grep ClientReceivedMessage | sed 's/\([^ ]*\) .*ClientReceivedMessage CallID=\([0-9]*\) .*/\1 \2/' > clientReceived.dat
cat FIVES.log | grep SentMessage | sed 's/\([^ ]*\) .*SentMessage CallID=\([0-9]*\) .*/\1 \2/' > serverSent.dat
./processDelay_attributes.py

cat Clients.log grep ClientFinishedMessageHandling | sed 's/.*MessageID=\([0-9]*\) FinishedTime=\([0-9\.]*\).*/\2/' > finishedTime.dat
cat Clients.log | grep MessageReceived | sed 's/.*MessageID=\([0-9]*\)[. ]* ReceivedTime=\([0-9\.]*\).*/\2/' > receivedTime.dat
echo "Done."