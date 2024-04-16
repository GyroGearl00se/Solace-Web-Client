# Solace Web Client

## Description
The Solace Web Client is using the .NET (C#) library and offers Queue browsing - (Publish/Subscribe to be implemented).


## Secure Connection (TLS)
To perform secure connections (tcps://) mount the certificate(s) from our desired endpoint(s) in the "/trustedca" directory.


### Docker

```
docker run -d 8080:8080 gyrogearl00se/solacequeuebrowser:latest
```
