#v0.9

import sys, json

def main():
    stdin = json.load(sys.stdin)
    if "Task" not in stdin:
        raise Exception('No "Task" field in stdin of task output filter script.')
    task = stdin["Task"]
    message = "No message"
    if "Message" in task:
        message = task["Message"]
    
    latency = -2
    throughput = -2
    time = -2
    detail = "[Message before filter]:\n" + str(message)
    
    if message:
        data = message.split('\n', 3)    
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
