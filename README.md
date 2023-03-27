# agilpay-client-csharp
Agilpay client class c#

# Client class for Agilpay REST API service  

The Web Services Module consists of web services accessible via HTTP / HTTPS, which can be used by external channels or systems to perform operations on the System on the Gateway server.

Web Services can be accessed through standard protocols, such as REST
Endpoint authentication uses OAUTH 2.0 standard

This repository includes 2 projects
* agilpay.client class: sample implementation to connect to Agilpay REST API
* console-sample: console application to test agilpay.client class


# Available endpoints

* OAuth Autentication (init)
* Authorize
* Balance

# Generating authentication tokens

For authentication, this information must be provided to the token endpoint. If the provided credentials are valid, the identity provider will issue a token to the requesting application.
* grant_type: client_credentials
* client_id: Uniquely identifies the client requesting the token
* client_secret: Password used to authenticate the token request

Each response message will the following data:
* access_token: unique value that must be sent on every api call as a bearer token
* token_type: bearer
* expires_in: token expiration time
