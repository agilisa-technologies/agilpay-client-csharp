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
* Balance
* Authorize
* AuthorizeToken

# Initializing library

For authentication, this information must be provided to the Init method, the identity provider will issue a token to the requesting application.
* client_id: Uniquely identifies the client requesting the token
* client_secret: Password used to authenticate the token request

`result = await client.Init(client_id, secret);`

