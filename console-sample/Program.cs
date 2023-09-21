using agilpay;
using agilpay.models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace TestTransaction
{
    class Program
    {
        public static string Url { get; set; }
        public static string Client_id { get; set; }
        public static string Secret { get; set; }

        static async Task Main(string[] args)
        {
            GetInitialData();

            // Singleton Sample
            //await agilpay.ApiClient.Initialize(_url, client_id, secret);
            //await ConsoleSample(agilpay.ApiClient.Instance);

            // Trasient Sample
            var client = new agilpay.ApiClient();
            _ = await client.Init(Url, Client_id, Secret);
            await ConsoleSample(client);
        }

        private static void GetInitialData(){
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Agilpay API Client. Test console\n");

            Console.Write("URL [https://sandbox-webapi.agilpay.net/]:");

            Url = Console.ReadLine();
            Url = string.IsNullOrEmpty(Url) ? "https://sandbox-webapi.agilpay.net/" : Url;


            ///var client = new agilpay.ApiClient(_url);

            // OAUTH 2.0
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Client_id [API-001]:");
            Client_id = Console.ReadLine();
            Client_id = string.IsNullOrEmpty(Client_id) ? "API-001" : Client_id;

            Console.Write("Secret [Dynapay]:");
            Secret = Console.ReadLine();
            Secret = string.IsNullOrEmpty(Secret) ? "Dynapay" : Secret;

            Console.ForegroundColor = ConsoleColor.Green;

        }

        private static async Task ConsoleSample(ApiClient client)
        {
            try
            {
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
                    Console.WriteLine("Customer Wallet:");
                    foreach (var item in resultTokens)
                    {
                        Console.WriteLine($"> {item.Account}");
                    }
                }
                else
                {
                    Console.WriteLine("No tokens found");
                }

                ///  Balance
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
                    AccountType = agilpay.AccountType.Credit_Debit,
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
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }
        }

    }
}
