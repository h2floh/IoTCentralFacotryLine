# IoTCentralFactoryLine
.NET Core program for a virtual production line for a IoTCentral Demo which also demonstrates the X509 CA auto provisioning feature

# Connection Options
It is possible to specify in App.config
  1) IoT Hub (X509 CA)
        ```xml
        <add key="IOT_HUB_URI" value="iothub url"/>
        <add key="DEVICE_CERTIFICATE" value="pfx"/>
        <add key="device_id" value="deviceid"/>
        ```
  2) IoT Central SAS Connection String
        ```xml
        <add key="device_conn_str" value="IoT Central SAS Connection string"/>
        ```
  3) Device Provisioning Service (X509 CA)
        ```xml
        <add key="DEVICE_CERTIFICATE" value="location of pfx"/>
        <add key="DPS_IDSCOPE" value="DPS scopeid"/>
        <add key="PASSWORD" value="password of certificate"/>
        ```
        
It is also possible to run as Docker Container
  1) Device Provisioning Service (X509 CA)
      you have to copy your pfx file(s) in the base image into the /app folder
      ```bash
      docker run -e "DEVICE_CERTIFICATE=location of pfx" -e "DPS_IDSCOPE=DPS scopeid" -e "PASSWORDpassword of certificate" <container-name>
      ```
# Prebuilt docker container
https://hub.docker.com/r/florianbespin/dev-factory-iotcentral/
```bash
docker pull florianbespin/dev-factory-iotcentral
```

# Build Docker container
```bash
docker build -t <dev-container-name> .
```

# Push Docker container to repo
```bash
docker tag <dev-container-name> <your-repository-name>/<container-name>
docker push <your-repository-name>/<container-name>
```

# Cleanup Docker build temp containers 
```bash
docker rm $(docker ps --all -q --no-trunc)
docker rmi $(docker images --filter "dangling=true" -q --no-trunc)
```
# Execute on ACI (Azure Container Instances)
```cli
az container create -g IoTCentralDemo -l japaneast -f ProductionGroupKorea.yaml
az container create -g IoTCentralDemo -l northeurope -f ProductionGroupGermany.yaml
```
