#!/bin/bash

# Copied instructions from https://jamielinux.com/docs/openssl-certificate-authority/introduction.html
# Thanks Jamie Nguyen!

# Generate private key
echo "Generating private key for CA..."
openssl genrsa -aes256 -out private/iotca.key.pem 4096

# Generate CA root certificate
echo "Generating CA root certificate..."
openssl req -config openssl.cnf -key private/iotca.key.pem -new -x509 -days 7300 -sha256 -extensions v3_ca -out certs/iotca.cert.pem
chmod 444 certs/iotca.cert.pem

# Verify CA root certificate
echo "Verify CA root certificate..."
openssl x509 -noout -text -in certs/iotca.cert.pem

# Create CA intermediate key
echo "Create CA intermediate key..."
openssl genrsa -aes256 -out intermediate/private/iotintermediate.key.pem 4096

# Create CA intermediate certificate
echo "Create CA intermediate certificate..."
openssl req -config openssl.cnf -new -sha256 -key intermediate/private/iotintermediate.key.pem -out intermediate/csr/iotintermediate.csr.pem

# Sign CA intermediate certificate with CA
echo "Sign CA intermediate certificate with CA..."
openssl ca -config openssl.cnf -extensions v3_intermediate_ca -days 3650 -notext -md sha256 -in intermediate/csr/iotintermediate.csr.pem -out intermediate/certs/iotintermediate.cert.pem

# Verify CA intermediate certificate
echo "Verify CA intermediate certificate..."
openssl x509 -noout -text -in intermediate/certs/iotintermediate.cert.pem
openssl verify -CAfile certs/iotca.cert.pem intermediate/certs/iotintermediate.cert.pem

# Create intermediate certificate chain
echo "Create intermediate certificate chain... (for use at IoT Hub/DPS)"
cat intermediate/certs/iotintermediate.cert.pem certs/iotca.cert.pem > intermediate/certs/iotca-chain.cert.pem

