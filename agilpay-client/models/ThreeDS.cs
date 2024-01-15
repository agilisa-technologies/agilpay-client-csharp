using System;
using System.Collections.Generic;
using System.Text;

namespace agilpay.models
{
    public class ThreeDS
    {
        public string authenticationValue { get; set; }
        public string eci { get; set; }
        public string status { get; set; }
        public string protocolVersion { get; set; }
        public string dsTransId { get; set; }
        public string acsTransId { get; set; }
        public string cardToken { get; set; }
        public bool scaIndicator { get; set; }

    }
}
