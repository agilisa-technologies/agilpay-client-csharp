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

                var _url = GetInput("URL [https://sandbox-webapi.agilpay.net/]:", "https://sandbox-webapi.agilpay.net/");
                var client = new ApiClient(_url);

                // OAUTH 2.0
                Console.ForegroundColor = ConsoleColor.White;
                var client_id = GetInput("Client_id [API-001]:", "API-001");
                var secret = GetInput("Secret [Dynapay]:", "Dynapay");

                await client.Init(client_id, secret);

                Console.ForegroundColor = ConsoleColor.White;
                var merchant_key = GetInput("Merchant Key [TEST-001]:", "TEST-001");

                string customer_id = await GetCustomerTokens(client);

                // Get Balance
                await GetBalance(client, merchant_key, customer_id);

                // Authorize Payment
                await AuthorizePayment(client, merchant_key, customer_id);

                Console.WriteLine("Press any key...");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }
        }

        private static async Task<string> GetCustomerTokens(ApiClient client)
        {
            var customer_id = GetInput("Customer Account [123456]:", "123456");

            var resultTokens = await client.GetCustomerTokens(customer_id);
            if (resultTokens != null && resultTokens.Count > 0)
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

            return customer_id;
        }

        private static async Task AuthorizePayment(ApiClient client, string merchant_key, string customer_id)
        {
            Console.ForegroundColor = ConsoleColor.White;
            var amount = GetInput("\nPayment Amount [1.02]:", "1.02");
            double.TryParse(amount, out double amountDouble);

            var card = GetInput("\nPayment Account [4242-4242-4242-4242]:", "4242424242424242");

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
                Amount = amountDouble,
                Currency = "840",
                Tax = 0.0,
                Invoice = "123465465",
                Transaction_Detail = "payment information detail"
            };

            Console.WriteLine("Requesting authorization...");
            var resultPayment = await client.AuthorizePayment(authorizationRequest);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Authorization Result:\n" + JsonConvert.SerializeObject(resultPayment, Formatting.Indented));
        }

        private static async Task GetBalance(ApiClient client, string merchant_key, string customer_id)
        {
            var balanceRequest = new BalanceRequest()
            {
                MerchantKey = merchant_key,
                CustomerId = customer_id,
            };

            var resultBalance = await client.GetBalance(balanceRequest);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Balance Result:\n" + JsonConvert.SerializeObject(resultBalance, Formatting.Indented));
        }   
        
        private static string GetInput(string prompt, string defaultValue)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            return string.IsNullOrEmpty(input) ? defaultValue : input;
        }
    }
}
