using System;
using System.Collections.Generic;
using System.Text;

namespace agilpay.models
{
    public class TempLinkCreateRequest
    {
        public string UniqueKey { get; set; }
        public decimal Amount { get; set; }
        public string MerchantKey { get; set; }
        public string SiteId { get; set; }
        public string Currency { get; set; } = "840";
        public object[] Items { get; set; } = new object[] { };
        public object[] Detalles { get; set; } = new object[] { };
        public DateTime? ExpirationDate { get; set; }
    }
}
