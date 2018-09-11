#!/bin/bash

# app_root is where Frontend.dll is.
app_root=${app_root:-$(pwd)}
cd $app_root

src_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

cert_path=$app_root/cert.pfx

msg='required but missing'

apibase=${apibase:?$msg} app_root=$app_root/wwwroot $src_root/config-portal.sh && \
  fqdn=${fqdn:?$msg} pfx_file=$cert_path $src_root/generate-cert.sh && \
  ServerOptions__CertPath=$cert_path dotnet Frontend.dll
