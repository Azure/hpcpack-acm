#!/bin/bash

# app_root is the portal root dir.
app_root=${app_root:-$(pwd)}
cd $app_root


if ! apibase=${apibase:?$msg} app_root=$app_root $src_root/config-portal.sh ; then
  exit false
fi

src_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
cert_file=$src_root/cert.crt
key_file=$src_root/cert.key

cert_file=$cert_file key_file=$key_file $src_root/generate-cert.sh && \
  server_name=${fqdn:?'required but missing'} cert_file=$cert_file key_file=$key_file $src_root/config-nginx.sh && \
  nginx
