#!/bin/bash

cd "$( dirname "${BASH_SOURCE[0]}" )"

fqdn=${fqdn:-$(hostname)}
pfx_file=${pfx_file:-'cert.pfx'}
cert_file=${cert_file:-'cert.crt'}
key_file=${key_file:-'cert.key'}

template_file=template-req.config
config_file="cert.config"

sed "s/\<DNS:LOCALHOST\>/DNS:${fqdn}/g" $template_file \
  | sed "s/\<KEY_FILE\>/${key_file//\//\\\/}/g" > $config_file

openssl req -config $config_file -new -out csr.pem \
  && openssl x509 -req -days 3650 -extfile $config_file -extensions v3_req -in csr.pem -signkey $key_file -out $cert_file \
  && openssl pkcs12 -export -out $pfx_file -inkey $key_file -in $cert_file -password pass:
