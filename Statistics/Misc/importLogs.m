% Load data

filename = 'D:\Projects\FIVES\Stat\Logs\clientSentMessages.dat';
delimiter = ' ';
formatSpec = '%s%s%f%[^\n\r]';
fileID = fopen(filename,'r');
dataArray = textscan(fileID, formatSpec, 'Delimiter', delimiter, 'MultipleDelimsAsOne', true, 'EmptyValue' ,NaN, 'ReturnOnError', false);
fclose(fileID);
dataArray{1} = datenum(dataArray{1}, 'HH:MM:SS.FFF');

times_csm = dataArray{:, 1};
funcs = dataArray{:, 2};
sizes = dataArray{:, 3};

filename = 'D:\Projects\FIVES\Stat\Logs\delay.dat';
delimiter = ' ';
formatSpec = '%s%f%f%[^\n\r]';
fileID = fopen(filename,'r');
dataArray = textscan(fileID, formatSpec, 'Delimiter', delimiter, 'MultipleDelimsAsOne', true, 'EmptyValue' ,NaN, 'ReturnOnError', false);
fclose(fileID);
dataArray{1} = datenum(dataArray{1}, 'HH:MM:SS.FFF');

times_d = dataArray{:, 1};
delays = dataArray{:, 2};
start_times = dataArray{:, 3};

filename = 'D:\Projects\FIVES\Stat\Logs\queueSize.dat';
delimiter = ' ';
formatSpec = '%s%f%[^\n\r]';
fileID = fopen(filename,'r');
dataArray = textscan(fileID, formatSpec, 'Delimiter', delimiter, 'MultipleDelimsAsOne', true, 'EmptyValue' ,NaN, 'ReturnOnError', false);
fclose(fileID);
dataArray{1} = datenum(dataArray{1}, 'HH:MM:SS.FFF');

times_qs = dataArray{:, 1};
queue_sizes = dataArray{:, 2};

clearvars filename delimiter formatSpec fileID dataArray ans;

% Rescale data

start_time = min([min(times_csm) min(times_d) min(times_qs)]);

times_csm = (times_csm - start_time) * 24*60*60;
times_d = (times_d - start_time) * 24*60*60;
times_qs = (times_qs - start_time) * 24*60*60;

% Plot data

clf;
hold on;
plot(times_csm, 10000, 'Color', 'green', 'Marker', 'x', 'LineStyle', 'none');
plot(times_d, delays, 'Color', 'blue', 'Marker', 'x', 'LineStyle', 'none');
plot(times_qs, queue_sizes, 'Color', 'red');
legend('client sends', 'queue size', 'client message delay');
hold off;