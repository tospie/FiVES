#!/usr/bin/python

delays = [float(x) for x in open('delay.dat').readlines()]

print "For entire data:"
print "  Min  = " + str(min(delays)) 
print "  Max  = " + str(max(delays)) 
print "  Avg  = " + str(sum(delays)/len(delays)) 


delays50 = delays[int(len(delays)/2):];
print "For second half of data:"
print "  Min50  = " + str(min(delays50)) 
print "  Max50  = " + str(max(delays50)) 
print "  Avg50  = " + str(sum(delays50)/len(delays)) 

delays.sort()
print "Messages sent in total: " + str(len(delays))
print "  Last 80 % are above " + str(delays[int(len(delays)*0.2)])
print "  Last 50 % are above " + str(delays[int(len(delays)*0.5)])
print "  Last 30 % are above " + str(delays[int(len(delays)*0.7)])
print "  Last 10 % are above " + str(delays[int(len(delays)*0.9)])
print "  Last  5 % are above " + str(delays[int(len(delays)*0.95)])