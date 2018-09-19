import psutil
import json

paths = [d.mountpoint for d in psutil.disk_partitions()]
usages = [psutil.disk_usage(p) for p in paths]
total = sum([u.free for u in usages])
result = { "_Total": total }
for idx in range(len(paths)):
    result[paths[idx]] = usages[idx].free
print(json.dumps(result))

