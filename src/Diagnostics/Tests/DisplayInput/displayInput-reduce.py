import sys, json, copy

def main():
    result = json.load(sys.stdin)
    print(json.dumps(result))
    return 0
        
def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))
    
if __name__ == '__main__':
    sys.exit(main())
