using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agilpay.models
{
    public class CaptureAdjustmendByIDRequest
    {
        public string MerchantKey { get; set; }
        public string IDTransaction { get; set; }
        public string Amount { get; set; }
    }
}
