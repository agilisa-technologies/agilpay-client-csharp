using System;
using System.Collections.Generic;
using System.Text;

namespace agilpay.models
{
    public class RefundByIdRequest
    {
        public string MerchantKey { get; set; }
        public string IDTransaction { get; set; }

        public string ExtData { get; set; }

        public decimal Amount { get; set; } = 0;
    }
}
