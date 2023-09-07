using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agilpay.models
{
    public class DeleteTokenRequest
    {
        public string AccountToken { get; set; }
        public string CustomerID { get; set; }
    }
}
