using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agilpay.client.models
{
    public class VoidSaleRequest
    {
        public string MerchantKey { get; set; }
        public string AuthNumber { get; set; }
        public string ReferenceCode { get; set; }
        public string AuditNumber { get; set; }
    }
}
