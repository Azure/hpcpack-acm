import sys, json, copy
sys.stdin.read()

task={}
task["Id"]=1
task["CommandLine"]="echo 'Success'"
task["Node"]="evancvmss"
task["CustomizedData"]="Distribution script stdin"
print(json.dumps([task]))
