import psutil
import json

mem = psutil.virtual_memory()
result = { "_Total": mem.percent }
print(json.dumps(result))
