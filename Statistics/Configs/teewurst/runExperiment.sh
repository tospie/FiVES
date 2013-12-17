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
echo "Starting clients..."
for i in $(seq -f "%05g" 1 $numClients); do
  screen -S client$i -d -m mono NativeClient.exe --logfile=Logs/Client$i.log
  sleep $start_delay
done
echo "Waiting for $duration seconds..."
echo "Clean test start: " $(date +%T.%N)
sleep $duration
echo "Clean test end: " $(date +%T.%N)
echo "Stopping clients..."
for i in $(seq -f "%05g" 1 $numClients); do
  screen -S client$i -X quit
done
echo "Experiment complete"

# Merging logs
echo "Merging logs..."
#tar -czf FIVES.log Logs
cat Logs/*.log | sort > FIVES.log
#cat Logs/*.log | egrep ".*CallDuration.*location.*" | sed 's/\([0-9][0-9]:[0-9][0-9]:[0-9][0-9]\) \[INFO\] {[^}]*} CallDuration=\([\.0-9]*\) FuncName=\(location.update[a-zA-Z]*\)/\1 \2 \3/' > Logs/data
echo "Done."
