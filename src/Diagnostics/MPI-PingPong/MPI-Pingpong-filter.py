#v0.12

import sys, json

def main():
    latency = -2
    throughput = -2
    time = -2
    data = sys.stdin.read()
    detail = '[Message before filter]:\n' + data
    data = data.split('\n', 3)
    
    if len(data) == 4:
        try:
            latency = float(data[0])
            throughput = float(data[1])
            time = float(data[2])
            detail = data[3]
        except:
            latency = throughput = time = -2            
            
    if len(detail) > 20000:
        detail = detail[:20000]
        detail += '\n...'

    result = {
        "Latency": latency,
        "Throughput": throughput,
        "Time": time,
        "Detail": detail
    }

    print(json.dumps(result))

if __name__ == '__main__':
    sys.exit(main())
