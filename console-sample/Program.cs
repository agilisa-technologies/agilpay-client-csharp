using agilpay.models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace TestTransaction
{
    class Program
    {

        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Agilpay API Client. Test console\n");

            var client = new agilpay.ApiClient("https://sandbox-webapi.agilpay.net/");

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
                result = await client.Init(client_id, secret);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OAuth 2.0 token:\n" + client.Token);
            }


            ///  Balance

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Merchant Key [TEST-001]:");

            var merchant_key = Console.ReadLine();
            merchant_key = string.IsNullOrEmpty(merchant_key) ? "TEST-001" : merchant_key;

            Console.Write("Customer Account [123456]:");
            var customer_id = Console.ReadLine();
            customer_id = string.IsNullOrEmpty(customer_id) ? "123456" : customer_id;

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
            card = string.IsNullOrEmpty(card) ? "4242424242424242": card;

            var authorizationRequest = new AuthorizationRequest()
            {
                MerchantKey = merchant_key,
                AccountNumber = card,
                ExpirationMonth = "01",
                ExpirationYear = "29",
                CustomerName = "Test User",
                CustomerID = customer_id,
                AccountType = "1", 
                CustomerEmail = "testuser@gmail.com",
                ZipCode = "33167",
                Amount = amount,
                Currency = "840",
                Tax = "0",
                Invoice = "123465465", 
                Transaction_Detail = "payment information detail"
            };

            Console.WriteLine("Requesting authorization...");
            var resultPayment = await client.Authorize(authorizationRequest);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Authorization Result:\n" + JsonConvert.SerializeObject(resultPayment, Formatting.Indented));

            Console.WriteLine("Press any key...");
            Console.ReadLine();
        }



    }
}
