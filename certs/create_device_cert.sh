#!/bin/bash

# Copied instructions from https://jamielinux.com/docs/openssl-certificate-authority/introduction.html
# Thanks Jamie Nguyen!

deviceid=""
while [ "$1" != "" ]; do
    case $1 in
        -f | --filename )       shift
                                deviceid=$1
                                ;;
    esac
    shift
done

if [ "$deviceid" == "" ]
then
    echo "--filename has to be provided."
    exit 1
fi

# Generate private key
echo "Generating private key..."
openssl genrsa -aes256 -out intermediate/private/$deviceid.key.pem 2048

# Generate Certificate
echo "Generating certificate"
openssl req -config intermediate/openssl.cnf -key intermediate/private/$deviceid.key.pem -new -sha256 -out intermediate/csr/$deviceid.csr.pem

#Sign with intermediate
echo "Sign Certificate with Intermediate CA Certificate"
openssl ca -config intermediate/openssl.cnf -extensions server_cert -days 3750 -notext -md sha256 -in intermediate/csr/$deviceid.csr.pem -out intermediate/certs/$deviceid.cert.pem

#Verify
echo "Verify Certificate..."
openssl verify -CAfile intermediate/certs/iotca-chain.cert.pem intermediate/certs/$deviceid.cert.pem

#Create PFX
echo "Create PFX file..."
openssl pkcs12 -export -in intermediate/certs/$deviceid.cert.pem -inkey intermediate/private/$deviceid.key.pem -out intermediate/pfx/$deviceid.pfx
