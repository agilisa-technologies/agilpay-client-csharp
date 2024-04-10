using System;

namespace agilpay.client.models
{
    public class AuthorizationTokenRequest
    {
        public string MerchantKey { get; set; }
        public string AccountToken { get; set; }

        public string CustomerID { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }

        public double Amount { get; set; }
        public string Currency { get; set; }
        public double Tax { get; set; }
        public string Invoice { get; set; }
        public string Transaction_Detail { get; set; }
        public bool HoldFunds { get; set; }
        public string CVV { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string ExtData { get; set; }
        public bool IsInstallments { get; set; }
        public int InstallmentsCount { get; set; }


    }
}
