cp -a 

cd FIVES1
screen -S experiment1 -d -m ./runExperiment.sh 50 1200 > experiment.log
cd ../FIVES2
screen -S experiment2 -d -m ./runExperiment.sh 50 1200 > experiment.log
cd ..
