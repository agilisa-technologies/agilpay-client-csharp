using System;

namespace agilpay.client.models
{
    public class AuthorizationRequest
    {
        public string MerchantKey { get; set; }
        public AccountType AccountType { get; set; }
        public string AccountNumber { get; set; }
        public string RoutingNumber { get; set; }
        public DateTime EffectiveDate { get; set; }
        public bool IsDefault { get; set; }
        public string ExpirationMonth { get; set; }
        public string ExpirationYear { get; set; }
        public string CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerState { get; set; }
        public string ZipCode { get; set; }
        public double Amount { get; set; }
        public string Currency { get; set; }
        public double Tax { get; set; }
        public string CVV { get; set; }
        public string Invoice { get; set; }
        public string Transaction_Detail { get; set; }
        public bool HoldFunds { get; set; }
        public string ExtData { get; set; }

        public bool SaveWallet { get; set; }
        public bool ForceDuplicate { get; set; }
    }
}
