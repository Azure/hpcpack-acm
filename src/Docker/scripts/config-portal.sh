#!/bin/bash

msg='required but missing'
# app_root is the portal root dir.
app_root=${app_root:?$msg}

mkdir -p $app_root/assets/environments
echo "{
\"name\": \"product\",
\"production\": true
}" > $app_root/assets/environments/environment.json
