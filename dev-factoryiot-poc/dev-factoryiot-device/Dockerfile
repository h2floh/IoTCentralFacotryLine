FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY dev_factoryiot_device.csproj dev-factoryiot-device/
RUN dotnet restore dev-factoryiot-device/dev_factoryiot_device.csproj
COPY . dev-factoryiot-device/
WORKDIR /src/dev-factoryiot-device
RUN dotnet build dev_factoryiot_device.csproj -c Release -o /app
COPY certs/* /app/

FROM build AS publish
RUN dotnet publish dev_factoryiot_device.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "dev_factoryiot_device.dll"]
