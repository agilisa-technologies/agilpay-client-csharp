using System;
using System.Collections.Generic;
using System.Text;

namespace agilpay.models
{
    public class RegisterTokenRequest
    {
        public string MerchantKey { get; set; }
        public string AccountType { get; set; } //credit or debit card = 1, ach-checking = 2, ach-savings = 3
        public string AccountNumber { get; set; }
        public string ExpirationMonth { get; set; }
        public string ExpirationYear { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public bool IsDefault { get; set; }
        public string ZipCode { get; set; }

        public string RoutingNumber { get; set; }
    }
}
