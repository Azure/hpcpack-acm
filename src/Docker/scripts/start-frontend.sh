#!/bin/bash

# app_root is where Frontend.dll is.
app_root=${app_root:-$(pwd)}
cd $app_root

src_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

msg='required but missing'

app_root=$app_root/wwwroot $src_root/config-portal.sh && \
  CloudOptions__Storage__AccountName=${APPSETTING_CloudOptions__Storage__AccountName:?$msg} \
  CloudOptions__Storage__KeyValue=${CUSTOMCONNSTR_CloudOptions__Storage__KeyValue:?$msg} \
  dotnet Frontend.dll
