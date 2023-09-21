using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace agilpay.client.models
{
    public class AuthorizationResponse
    {
        public string Account { get; set; } = string.Empty;
        public string AccountToken { get; set; } = string.Empty;
        public string IDTransaction { get; set; } = "0";
        public string BatchCode { get; set; } = string.Empty;
        public string AcquirerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string AuditNumber { get; set; }
        public string ResponseCode { get; set; }
        public string DesicionResponseCode { get; set; }
        public string Message { get; set; }
        public string AuthNumber { get; set; }
        public string HostDate { get; set; }
        public string HostTime { get; set; }
        public string Reference_Code { get; set; }
        public decimal Amount { get; set; }
        public short Currency { get; set; }
    }
}
