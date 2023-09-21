
using agilpay;
using agilpay.client.models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace TestTransaction
{
    class Program
    {

        static async Task Main(string[] args)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Agilpay API Client. Test console\n");

                Console.Write("URL [https://sandbox-webapi.agilpay.net/]:");

                var _url = Console.ReadLine();
                _url = string.IsNullOrEmpty(_url) ? "https://sandbox-webapi.agilpay.net/" : _url;

                

                var client = new ApiClient(_url);

                // OAUTH 2.0
                bool result = false;
                while (!result)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Client_id [API-001]:");
                    var client_id = Console.ReadLine();
                    client_id = string.IsNullOrEmpty(client_id) ? "API-001" : client_id;

                    Console.Write("Secret [Dynapay]:");
                    var secret = Console.ReadLine();
                    secret = string.IsNullOrEmpty(secret) ? "Dynapay" : secret;
                    await client.Init(client_id, secret);

                    Console.ForegroundColor = ConsoleColor.Green;
                    //Console.WriteLine("OAuth 2.0 token:\n" + client.Token);

                    //await ApiClient.Initialize(_url, client_id, secret);
                    break;
                }


                ///  Balance

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Merchant Key [TEST-001]:");

                var merchant_key = Console.ReadLine();
                merchant_key = string.IsNullOrEmpty(merchant_key) ? "TEST-001" : merchant_key;

                Console.Write("Customer Account [123456]:");
                var customer_id = Console.ReadLine();
                customer_id = string.IsNullOrEmpty(customer_id) ? "123456" : customer_id;

                var resultTokens = await client.GetCustomerTokens(customer_id);
                if (resultTokens != null)
                {
                    foreach (var item in resultTokens)
                    {
                        Console.WriteLine($"{item.Account}");
                    }
                }
                else
                {
                    Console.WriteLine("No tokens found");
                }


                var balanceRequest = new BalanceRequest()
                {
                    MerchantKey = merchant_key,
                    CustomerId = customer_id,
                };

                var resultBalance = await client.GetBalance(balanceRequest);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Balance Result:\n" + JsonConvert.SerializeObject(resultBalance, Formatting.Indented));

                // PAYMENT

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\nPayment Amount [1.02]:");
                var amount = Console.ReadLine();
                amount = string.IsNullOrEmpty(amount) ? "1.02" : amount;

                Console.Write("\nPayment Account [4242-4242-4242-4242]:");
                var card = Console.ReadLine();
                card = string.IsNullOrEmpty(card) ? "4242424242424242" : card;

                var authorizationRequest = new AuthorizationRequest()
                {
                    MerchantKey = merchant_key,
                    AccountNumber = card,
                    ExpirationMonth = "01",
                    ExpirationYear = "29",
                    CustomerName = "Test User",
                    CustomerID = customer_id,
                    AccountType = AccountType.Credit_Debit,
                    CustomerEmail = "testuser@gmail.com",
                    ZipCode = "33167",
                    Amount = amount,
                    Currency = "840",
                    Tax = "0",
                    Invoice = "123465465",
                    Transaction_Detail = "payment information detail"
                };

                Console.WriteLine("Requesting authorization...");
                var resultPayment = await client.AuthorizePayment(authorizationRequest);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Authorization Result:\n" + JsonConvert.SerializeObject(resultPayment, Formatting.Indented));

                Console.WriteLine("Press any key...");
                Console.ReadLine();
            }catch(Exception e)
            {
                Console.Write(e.Message);
            }
        }



    }
}
