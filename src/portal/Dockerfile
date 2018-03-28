# This is the Dockerfile for building dev/test box.

FROM teracy/angular-cli:1.5.0

RUN \
  wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
  && sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google-chrom.list' \
  && apt-get update && apt-get install -y google-chrome-stable

RUN groupadd -r dev && useradd --no-log-init -r -m -s /bin/bash -g dev dev
