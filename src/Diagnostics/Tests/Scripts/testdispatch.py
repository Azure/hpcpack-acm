import sys
sys.stdin.read()
print("""[{\"JobId\":5
	,\"Id\":1,\"RequeueCount\":0,
	\"JobType\":1,\"ParentIds\":null,
	\"ChildIds\":null,\"RemainingParentIds\":null,
	\"CommandLine\":\"env & sleep 10 & hostname & date\",
	\"Node\":\"evancvmss000001\",
	\"UserName\":\"testusername\",
	\"EnvironmentVariables\": { \"TestVar1\":\"testvalue1\", \"TestVar2\": \"testvalue2\" },
	\"Password\":null,\"PrivateKey\":\"-----BEGIN RSA PRIVATE KEY-----\\nMIIEowIBAAKCAQEA06bdmM5tU/InWfakBnAltIA2WEvuZ/3qFwaT4EkmgJuEITxi+3NnXn7JfW+q\\n6ezBc4lx6J0EuPggDIcslbczyz65QrB2NoH7De1PiRtWNWIonQDZHTYCbnaU3f/Nzsoj62lgfkSf\\nUj4Osxd0yHCuGsfCtKMDES3d55RMUdVwbrXPL8jUqA9zh4miV9eX0dh/+6pCqPD7/dnOCy/rYtXs\\nwgdjKG57O6eaT3XxiuozP00E5tZ7wF0fzzXBuA2Z21Sa2U42sOeeNuOvOuKQkrIzprhHhpDik31m\\nHZK47F7eF2i7j/0ImedOFdgA1ETPPKFLSGspvf1xbHgEgGz1kjFq/QIEAAEAAQKCAQABHZ2IW741\\n7RKWsq6J3eIBzKRJft4J7G3tvJxW8e3nOpVNuSXEbUssu/HUOoLdVVhuHPN/1TUgf69oXTtiRIVc\\nLIPNwcrGRGHwaP0JJKdY4gallLFMCB9i5FkhnJXbaiJvq+ndoqnnAPLf9GfVDqhV5Jqc8nxeDZ2T\\nks037GobtfMuO5WeCyTAMzc7tDIsn0HGyV0pSa7JFHAKorUuBMNnjEM+SBL37AqwcVFkstC4YD3I\\n7j4miRE3loxPmBJs5HMTV4jpAGNbNmrPzfrmP4swHNoc9LR7YKpfzVpAzb24QY82fewvOxRZH6Hz\\nBVhueJZAGV62JbBeaw9eeujDp+UBAoGBAN6IanPN/wqdqQ0i5dBwPK/0Mcq6bNtQt6n8rHD/C/xL\\nFuhuRhLPI6q7sYPeSZu84EjyArLMR1o0GW90Ls4JzIxjxGCBgdUHG8YdmB9jNIjR5notYQcRNxaw\\nwLuc5nurPt7QaxvqO3JcaDbw9c6q9c7xNE3Wlak4xxKeiXsWyHQdAoGBAPN7hpqISKIc+8dPc5kg\\nuJDDPdFcJ8py0nbysEYtY+hUaDxfw7Cm8zrNj+M9BbQR9yM6EW16P0FQ+/0XBrLMVpRkyJZ0Y3Ij\\n5Qol5IxJPyWzfj6e7cd9Rkvqs2sQcBehXCbQHjfpB12yu3excQBPT0Lr5gei7yfc+D21hGWDH1xh\\nAoGAM2lm1qxf4O790HggiiB0FN6g5kpdvemPFSm4GT8DYN1kRHy9mbjbb6V/ZIzliqJ/Wrr23qIN\\nVgy1V6eK7LUc2c5u3zDscu/6fbH2pEHCMF32FoIHaZ+Tj510WaPtJ+MvWkDijgd2hnxM42yWDZI3\\nygC16cnKt9bTPzz7XEGuPA0CgYBco2gQTcAM5igpqiIaZeezNIXFrWF6VnubRDUrTkPP9qV+KxWC\\nldK/UczoMaSE4bz9Cy/sTnHYwR5PKj6jMrnSVhI3pGrd16hiVw6BDbFX/9YNr1xa5WAkrFS9bJCp\\nfPxZzB9jOGdUEBfhr4KGEqbemHB6AVUq/pj4qaKJGP2KoQKBgFt7cqr8t+0zU5ko+B2pxmMprsUx\\nqZ3mBATMD21AshxWxsawpqoPJJ3NTScNrjISERQ6RG3lQNv+30z79k9Fy+5FUH4pvqU4sgmtYSVH\\nM4xW+aJnEhzIbE7a4an2eTc0pAxc9GexwtCFwlBouSW6kfOcMgEysoy99wQoGNgRHY/D\\n-----END RSA PRIVATE KEY-----\",
	\"PublicKey\":null},
	{\"JobId\":5,\"Id\":2,\"RequeueCount\":0,\"JobType\":1,\"ParentIds\":[1],\"ChildIds\":null,\"RemainingParentIds\":[],
	\"CommandLine\":\"hostname & date & env\",\"Node\":\"evancvmss000002\",\"UserName\":null,\"Password\":null,\"PrivateKey\":null,\"PublicKey\":null}]""")
