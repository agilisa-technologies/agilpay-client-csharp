using System;
using System.Collections.Generic;
using System.Text;

namespace agilpay.models
{
    public class RefundByIdRequest
    {
        public string Merchantkey { get; set; }
        public int IDTransaction { get; set; }

        public string ExtData { get; set; }

        public decimal Amount { get; set; } = 0;
    }
}
