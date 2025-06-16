
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
                    break;
                }
                ///  Balance

                ///Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Merchant Key [TEST-001]:");

                var merchant_key = Console.ReadLine();
                merchant_key = string.IsNullOrEmpty(merchant_key) ? "TEST-001" : merchant_key;

                Console.Write("Customer Account [010957593]:");
                var customer_id = Console.ReadLine();
                customer_id = string.IsNullOrEmpty(customer_id) ? "010957593" : customer_id;

                Console.WriteLine();
                Console.Write("Register token? Y/N: ");

                var go = Console.ReadLine();

                if (go.ToUpper().Trim() == "Y")
                {

                    var newToken = await client.RegisterToken(new agilpay.models.RegisterTokenRequest
                    {
                        CustomerId = customer_id,
                        AccountType = "1",
                        AccountNumber = "5252525252525252",
                        CVV = "123",
                        CustomerEmail = "test@gmail.com",
                        CustomerName = "pruebas",
                        ExpirationMonth = "07",
                        ExpirationYear = "2028",
                        MerchantKey = merchant_key,
                        NameOnAccount = "pruebas",
                        ZipCode = "12345"

                    });

                    Console.WriteLine("new token: " + JsonConvert.SerializeObject(newToken));
                }

                Console.WriteLine("Customer tokens:");
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

               


            }
            catch(Exception e)
            {
                Console.Write(e.Message);
            }





        }



    }
}
