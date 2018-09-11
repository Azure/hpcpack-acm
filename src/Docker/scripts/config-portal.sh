#!/bin/bash

msg='required but missing'
# app_root is the portal root dir.
app_root=${app_root:?$msg}
api_base=${apibase:?$msg}

mkdir -p $app_root/assets/environments
echo "{
\"name\": \"product\",
\"production\": true,
\"apiBase\": \"$api_base\"
}" > $app_root/assets/environments/environment.json
