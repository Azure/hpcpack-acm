import psutil
import json

mem = psutil.virtual_memory()
result = { "_Total": mem.available }
print(json.dumps(result))
