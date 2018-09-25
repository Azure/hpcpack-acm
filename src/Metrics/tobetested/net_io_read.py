import psutil
import json
import os
from datetime import datetime

TS_FORMAT = '%Y-%m-%dT%H:%M:%S.%f'

def get_data_file_path():
    try:
        path = os.environ['DATA_FILE']
    except:
        path = '/var/opt/net_io_read.data'
    return path

def read_data(path):
    try:
        with open(path, 'r') as f:
            lines = f.readlines()
    except IOError:
        result = (None, None)
    else:
        result = (int(lines[0].rstrip()), datetime.strptime(lines[1].rstrip(), TS_FORMAT))
    return result

def write_data(path, data):
    with open(path, 'w') as f:
        f.write('%s\n' % data[0])
        f.write('%s\n' % data[1].strftime(TS_FORMAT))

def try_write_data(path, data):
    try:
        write_data(path, data)
    except IOError:
        dir = os.path.dirname(path)
        os.makedirs(dir)
        write_data(path, data)

def compute():
    path = get_data_file_path()
    data, ts = read_data(path)
    counter = psutil.net_io_counters()
    now = datetime.utcnow()
    if data:
        result = { "_Total": counter.bytes_recv - data, "_Span": (now - ts).total_seconds() }
    else:
        result = { "_Total": 0, "_Span": 0 }
    try_write_data(path, (counter.bytes_recv, now))
    return result

def main():
    r = compute()
    print(json.dumps(r))

if __name__ == '__main__':
    main()

