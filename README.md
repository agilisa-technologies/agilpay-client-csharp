# agilpay-client-csharp


# Client class for Agilpay REST API service  
Agilpay client class c#

Agilpay REST API can be accessed through standard protocols such as REST. 
This library can be used by applications to perform operations on the Agilpay Gateway server using C# code

Endpoint authentication uses OAUTH 2.0 standard

This repository includes 2 projects
* agilpay.client class: sample implementation to connect to Agilpay REST API
* console-sample: console application to test agilpay.client class
 
Client library available on NuGet: https://www.nuget.org/packages/AgilPay.Client/

Complete documentation can be found at: https://agilisa.atlassian.net/wiki/x/AYAN

# Available endpoints

* OAuth Autentication (init)
* Balance
* AuthorizePayment
* AuthorizePaymentToken
* GetCustomerTokens
* IsValidCard
* IsValidRoutingNumber
* DeleteCustomerCard
* CloseBatchResumen
* VoidById
* VoidSale
* CaptureByID
* CaptureAdjustmendByID
* GetTransactionByID
* RecurringScheduleAdd
* RecurringScheduleGet
* RecurringScheduleChangeStatus
* RecurringScheduleUpdate
* RegisterToken

# Initializing library

The environment URL must be supplied on class initialization
* for test environment: https://sandbox-webapi.agilpay.net/ 
* for production environment: https://webapi.agilpay.net/ 

``` csharp
    _url = "https://sandbox-webapi.agilpay.net/";
    var client = new agilpay.ApiClient(_url);
```
> URL address could change depending on configuration. Please check with your account representative

For authentication, this information must be provided to the Init method, the identity provider will issue a token to the requesting application.
* client_id: Uniquely identifies the client requesting the token
* client_secret: Password used to authenticate the token request

``` csharp
    await client.Init(client_id, secret);
```

# Authorizing Payment
```
    var authorizationRequest = new AuthorizationRequest()
                {
                    MerchantKey = "TEST-001",
                    AccountNumber = "4242424242424242",
                    ExpirationMonth = "01",
                    ExpirationYear = "29",
                    CustomerName = "Test User",
                    CustomerID = "USER-123456",
                    AccountType = AccountType.Credit_Debit,
                    CustomerEmail = "testuser@gmail.com",
                    ZipCode = "33167",
                    Amount = 22.56,
                    Currency = "840",
                    Tax = "0",
                    Invoice = "123465465",
                    Transaction_Detail = "payment information detail"
                };

    Console.WriteLine("Requesting authorization...");
    var resultPayment = await client.AuthorizePayment(authorizationRequest);
```
