# sumo tum main campus

This is the repo containing the sumo project for the main campus including the opendrive map file

## sumo file preparation (tum-vt process)

Our workflow integrates Mathworks roadrunner to generate OpenDRIVE 1.7 files. These must be converted to sumo files using the following command:
```
netconvert --opendrive .\TUM_009.xodr -o tum_009.net.xml
```
Then the usual demand setup process in sumo can be done. 



create random demand and convert it:
```
python "C:\Program Files (x86)\Eclipse\Sumo\tools\randomTrips.py" -n .\tum_009.net.xml



duarouter -n .\tum_009.net.xml -t .\trips.trips.xml -o random_demand.rou.xml
```