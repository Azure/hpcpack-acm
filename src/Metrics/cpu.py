import psutil
import json
print(json.dumps(dict([ ("_"+str(index), s) for index, s in enumerate(psutil.cpu_percent(percpu=True,interval=0.1)) ] + [ ("_Total", psutil.cpu_percent()) ])))
