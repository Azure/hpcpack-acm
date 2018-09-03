import psutil
import json
cpuUsages = psutil.cpu_percent(percpu=True, interval=0.1)
_Total = round(sum(cpuUsages) / len(cpuUsages), 2)
print(json.dumps(dict([ ("_"+str(index), s) for index, s in enumerate(cpuUsages) ] + [ ("_Total", _Total) ])))