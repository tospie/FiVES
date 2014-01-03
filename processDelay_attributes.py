#!/usr/bin/python

delays = [float(x) for x in open('delay.dat').readlines()]
attributeDelays= [float(y) for y in open('attributeDelay.dat').readlines()]
queueTimes = [float(z) for z in open('queueProcessing.dat').readlines()]
attributeDelays.sort()
delays.sort()

print "For entire data:"
print "  Min (message / attribute) = " + str(min(delays)) + "/" + str(min(attributeDelays))
print "  Max (message / attribute) = " + str(max(delays)) + "/" + str(max(attributeDelays))
print "  Avg (message / attribute) = " + str(sum(delays)/len(delays)) + "/" + str(sum(attributeDelays)/len(attributeDelays))


delays50 = delays[int(len(delays)/2):];
attributeDelays50 = attributeDelays[int(len(attributeDelays)/2):];
print "For second half of data:"
print "  Min50 (message / attribute) = " + str(min(delays50)) + "/" + str(min(attributeDelays50))
print "  Max50 (message / attribute) = " + str(max(delays50)) + "/" + str(max(attributeDelays50))
print "  Avg50 (message / attribute) = " + str(sum(delays50)/len(delays)) + "/" + str(sum(attributeDelays50)/len(attributeDelays50))

print "Messages sent in total: " + str(len(delays))
print "  Last 80 % are above " + str(delays[int(len(delays)*0.2)])
print "  Last 50 % are above " + str(delays[int(len(delays)*0.5)])
print "  Last 30 % are above " + str(delays[int(len(delays)*0.7)])
print "  Last 10 % are above " + str(delays[int(len(delays)*0.9)])

print "Time to process incoming message:"
print "  Last 80 % are above " + str(attributeDelays[int(len(attributeDelays)*0.2)])
print "  Last 50 % are above " + str(attributeDelays[int(len(attributeDelays)*0.5)])
print "  Last 30 % are above " + str(attributeDelays[int(len(attributeDelays)*0.7)])
print "  Last 10 % are above " + str(attributeDelays[int(len(attributeDelays)*0.9)])

queueTimes.sort();
print "Time to enter UpdateQueue:"
print "  Last 80 % are above " + str(queueTimes[int(len(queueTimes)*0.2)])
print "  Last 50 % are above " + str(queueTimes[int(len(queueTimes)*0.5)])
print "  Last 30 % are above " + str(queueTimes[int(len(queueTimes)*0.7)])
print "  Last 10 % are above " + str(queueTimes[int(len(queueTimes)*0.9)])
