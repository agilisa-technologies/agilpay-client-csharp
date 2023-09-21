using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agilpay.client.models
{

    public class BalanceResponse
    {
        public string ResponseCode { get; set; }
        public string Message { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public CustomerAddress CustomerAddress { get; set; }
        public string TotalAmount { get; set; }
        public string MaxAmount { get; set; }
        public string MinAmount { get; set; }
        public Dictionary<String, String> ExtData { get; set; }
        public List<Invoice> Invoices { get; set; }
        public List<LastTransaction> Last_Transactions { get; set; }

    }
}
