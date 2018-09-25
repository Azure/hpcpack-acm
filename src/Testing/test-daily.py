import subprocess, os, time, shutil, smtplib, socket, argparse, json
from email.mime.application import MIMEApplication
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from email.header import Header
from email.utils import COMMASPACE, formatdate

def main(cluster, runtime, mail):
    for script in ['test-functional.py', 'test-restapi-get.py']:
        if not os.path.isfile(script):
            print '{} is not in current work directory {}'.format(script, os.getcwd())
            return
        
    CPU_COMMAND = "cores=$((`grep -c ^processor /proc/cpuinfo`-1)) && for i in `seq 1 $cores`; do while : ; do : ; done & done && sleep {} && for i in `seq 1 $cores`; do kill %$i; done".format(30)

    # 3 test batches currently
    commands = [
        [
            'test-functional.py {} --category diag-pingpong-tournament --timeout 2000 --continuous {}'.format(cluster, runtime),
        ],
        [
            'test-functional.py {} --category diag-pingpong-parallel --timeout 2000 --continuous {}'.format(cluster, runtime),
        ],
        [
            'test-functional.py {} --category clusrun --command "echo -n test" --result "test" --timeout 200 --continuous {}'.format(cluster, runtime),
        ],
        [
            'test-functional.py {} --category clusrun --command "ping localhost" --cancel 10 --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category clusrun --command "ping localhost" --cancel 30 --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category clusrun --command "ping localhost" --cancel 60 --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category clusrun --command "sleep 10" --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category clusrun --command "sleep 30" --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category clusrun --command "sleep 60" --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category clusrun --command "echo -n test" --result "test" --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category clusrun --command "whoami" --result "root\\n" --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category clusrun --command "hostname" --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category diag-pingpong-tournament --timeout 2000 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category diag-pingpong-parallel --timeout 2000 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category diag-pingpong-tournament --cancel 10 --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-functional.py {} --category diag-pingpong-parallel --cancel 30 --timeout 200 --continuous {}'.format(cluster, runtime),
            'test-restapi-get.py {} --continuous {}'.format(cluster, runtime)
        ],
    ]

    startTime = formatdate(localtime=True)

    # adapt to different OS
    runOnWindows = ''
    runOnLinux = 'python '
    if os.name == 'nt':
        prefix = runOnWindows
    else:
        prefix = runOnLinux
    commands = [[prefix + command for command in batch] for batch in commands]

    # create log directory
    logDir = '{}/test_logs/{}'.format(os.getcwd(), time.time())
    os.makedirs(logDir)
    logs = [['batch{}-thread{}.log'.format(j, i) for i in range(len(commands[j]))] for j in range(len(commands))]

    # start and wait test threads per batch
    for i in range(len(commands)):
        threads = [subprocess.Popen(commands[i][j], shell = True, stdout = open('{}/{}'.format(logDir, logs[i][j]), 'w'), stderr = subprocess.STDOUT) for j in range(len(commands[i]))]
        wait = [thread.wait() for thread in threads]

    # get the results from logs
    results = {}
    for log in os.listdir(logDir):
        with open('{}/{}'.format(logDir, log), 'r') as f:
            result = f.readlines()[-1]
        results[log] = result

    endTime = formatdate(localtime=True)
    
    mailBody = '<h4>Time:</h4>' + '<b>{}</b> &nbsp&nbsp - &nbsp&nbsp <b>{}</b>'.format(startTime, endTime) \
             + '<h4><br/>Results:</h4>' + '<br/><br/>'.join(['<br/>'.join(['{}: {}'.format(log, results[log]) for log in batch]) for batch in logs]) \
             + '<h4><br/>Details:</h4>' + '<br/><br/>'.join(['<br/>'.join(["<b>Log file</b>: {}".format(logs[i][j]), \
                                                                           "<b>Command</b>: {}".format(commands[i][j]), \
                                                                           "<b>Result</b>: {}".format(results[logs[i][j]])]) for i in range(len(commands)) for j in range(len(commands[i]))])
    if mail:
        # send notification mail
        shutil.make_archive(logDir, 'zip', logDir)
        with open(logDir+'.zip', 'rb') as f:
            attachment = MIMEApplication(f.read())
        attachment['Content-Disposition'] = 'attachment; filename="logs.zip"'
        sender = mail['Sender']
        to = mail['To']
        cc = mail['Cc']
        receivers = to + cc
        message = MIMEMultipart()
        message['From'] = Header(socket.gethostname(), 'utf-8')
        message['To'] = COMMASPACE.join(to)
        message['Cc'] = COMMASPACE.join(cc)
        message['Subject'] = 'Continuous functional test result for cluster {}'.format(cluster)
        message.attach(MIMEText(mailBody, 'html'))
        message.attach(attachment)
        smtp = smtplib.SMTP(mail['SmtpServer'])
        smtp.starttls()
        smtp.ehlo()
        smtp.login(mail['UserName'], mail['Password'])
        smtp.sendmail(sender, receivers, message.as_string())
    else:
        with open(logDir+'.html', 'w') as f:
            f.write(mailBody)

if __name__ == '__main__':
    def check_positive(value):
        ivalue = int(value)
        if ivalue < 0:
             raise argparse.ArgumentTypeError("{} is an invalid positive int value".format(ivalue))
        return ivalue
                          
    parser = argparse.ArgumentParser(description='Daily functional test aganist clusrun, diagnostics and job cancellation')
    parser.add_argument('cluster_uri', help='Specify the cluster to test')
    parser.add_argument('-r', '--runtime', type=check_positive, default=300, help='Specify the test duration as seconds for each test batch')
    parser.add_argument('-m', '--mail', type=argparse.FileType('r'), default=None, help='Specify the mail setting json file')

    args = parser.parse_args()

    mail = None
    if args.mail:
        with args.mail as f:
            mail = json.load(f)

    main(args.cluster_uri, args.runtime, mail)
    
