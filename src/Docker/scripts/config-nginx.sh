#!/bin/bash

msg='required but missing'
server_name=${server_name:?$msg}
cert_file=${cert_file:?$msg}
key_file=${key_file:?$msg}

cd "$( dirname "${BASH_SOURCE[0]}" )"

template=./template-site.conf

config=/etc/nginx/conf.d/a-site.conf

sed "s/\<SERVER_NAME\>/${server_name}/g" $template \
  | sed "s/\<SSL_CERTIFICATE\>/${cert_file//\//\\\/}/g" \
  | sed "s/\<SSL_CERTIFICATE_KEY\>/${key_file//\//\\\/}/g" > $config

nginx -t
