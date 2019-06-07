# Creating certificates
This tool should speed up X.509 certificate creation for IoT Hub / IoT Central and Device Provisioning Service.

Thanks to Jamie Nguyen blog with the openssl instructions to do it https://jamielinux.com/docs/openssl-certificate-authority/introduction.html

# Prerequisites
Linux (WSL, Ubuntu etc.) with installed openssl

# Changing defaults for Distinguished Name
You can change the defaults for your **_Distinguished Names_** (DN) under the **[req_distinguished_name]** section in `openssl.cnf` and `intermediate/openssl.cnf`.
For more information see: https://en.wikipedia.org/wiki/Certificate_signing_request

Use following CommonNames for:

| Certificate        | Common Name Value  |
| ------------------ |:------------------:|
| CA                 | CA                 |
| Intermediate       | intermediateCA     |
| Device             | "deviceId"         |
| Azure Verification | "VerificationCode" |


# Creation steps
Clone this repository within a linux shell. 

You have to provide passwords for your private keys and the values for the **_Distinguished Names_** (DN).

1. For creation of CA and Intermediate you will need to provide passwords as well as the DN information.
    ```bash
    ./create_ca_and_intermediate.sh 
    ```
2. For creation of the device certificates you need to provide a _filename_, password as well as the DN information. Try to stick to the rule _filename_ = _deviceId_ = _Common Name_
    ```bash
    ./create_device_cert.sh --filename <filename> 
    ```

# Registering Intermediate Certificate in IoT Hub / DPS / IoT Central
You can mainly use the tutorial provided here: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-security-x509-get-started

1. Certificate .pem file to use `./intermediate/certs/iotca-chain.cert.pem`
2. Generate the Verification Code and use it as CommonName while executing `./create_device_cert.sh --filename azureverify`
3. Upload/Verify with `./intermediate/certs/azureverify.cert.pem`

# Using device certificates
Within your device's or device simulation code/containers use the .pfx files automatically generated with the scripts

`./intermediate/pfx/<filename/devicename>.pfx`

# TODO / Improvements
Modify scripts to enter password / DN info automatically
