#!/usr/bin/python

from datetime import datetime
from datetime import timedelta

cr = {int(x.split(' ')[1]): datetime.strptime(x.split(' ')[0], '%H:%M:%S.%f') for x in open('clientReceived.dat').readlines()}
ss = {int(x.split(' ')[1]): datetime.strptime(x.split(' ')[0], '%H:%M:%S.%f') for x in open('serverSent.dat').readlines()}

delays=[]
for e in cr:
  delays.append(cr[e]-ss[e]);

print "Max = ", max(delays)
print "Min = ", min(delays)

adelay = 0
for d in delays:
  adelay += d.total_seconds()*1000

print "Average = ", adelay/len(delays)