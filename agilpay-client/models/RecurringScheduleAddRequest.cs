using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agilpay.models
{
    public class RecurringScheduleAddRequest
    {
        public string MerchantKey { get; set; }
        public string Service { get; set; }
        public string CustomerId { get; set; }
        public string AccountToken { get; set; }
        public double Amount { get; set; }
        public string Currency { get; set; }
        public int Period { get; set; }
        public int Frequency { get; set; }
        public int Day { get; set; }
        public int Quantity { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }
}
