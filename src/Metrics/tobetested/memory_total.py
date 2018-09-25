import psutil
import json

mem = psutil.virtual_memory()
result = { "_Total": mem.total }
print(json.dumps(result))
