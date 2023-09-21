using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agilpay.client.models
{
    public class CustomerToken
    {
        public string Account { get; set; }
        public string AccountToken { get; set; }
        public string CustomerName { get; set; }
        public string CustomerID { get; set; }
        public bool IsDefault { get; set; }
        public string Status { get; set; }
        public string AccountType { get; set; }
    }
}
