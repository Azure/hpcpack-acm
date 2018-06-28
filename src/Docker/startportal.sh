#!/bin/bash
if [ -n "$apibase" ]; then
	mkdir /app/portal/assets/environments
	echo "{ \
		\"name\": \"product\", \
		\"production\": true, \
		\"apiBase\": \"$apibase\" \
	}" > /app/portal/assets/environments/environment.json
fi
nginx
