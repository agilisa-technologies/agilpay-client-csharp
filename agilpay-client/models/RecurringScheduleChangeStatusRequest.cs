using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agilpay.models
{
    public class RecurringScheduleChangeStatusRequest
    {
        public string MerchantKey { get; set; }
        public string Service { get; set; }
        public string CustomerId { get; set; }
        public string Status { get; set; }
    }
}
