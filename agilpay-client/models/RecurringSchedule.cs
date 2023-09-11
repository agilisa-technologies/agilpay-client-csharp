using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agilpay.models
{
    public class RecurringSchedule
    {
        public string? ResponseCode { get; set; }
        public string? Message { get; set; }
        public string? AccountToken { get; set; }
        public string? Account { get; set; }
        public string? Period { get; set; }
        public string? Frequency { get; set; }
        public string? Day { get; set; }
        public string? Quantity { get; set; }
        public double? Amount { get; set; }
        public string? Currency { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? LastProcessDate { get; set; }
        public List<Transaction>? Transactions { get; set; }
        public string? RemainingTries { get; set; }
        public string? Status { get; set; }

    }
}
