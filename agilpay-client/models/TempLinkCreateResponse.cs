using System;
using System.Collections.Generic;
using System.Text;

namespace agilpay.models
{
    public class TempLinkCreateResponse
    {
        public string Key { get; set; }
        public string Link { get; set; }
        public string Status { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
