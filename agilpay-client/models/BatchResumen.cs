using System;
using System.Collections.Generic;
using System.Text;

namespace agilpay.models
{
    public class BatchResumen
    {
        public int IDAfiliado { get; set; }
        public string Merchant_code { get; set; }
        public string Terminal_code { get; set; }
        public int Sale_Count { get; set; }
        public double Sale_Total { get; set; }
        public int Refund_Count { get; set; }
        public double Refund_Total { get; set; }
        public int Void_Count { get; set; }
        public double Void_Total { get; set; }
        public int IDTransaccion_Max { get; set; }
        public string Batch_Code { get; set; }
        public string IDGroup { get; set; }
        public int IDBatch;
        public string ResponseCode;
        public string ResponseMessage;
        public string AcquirerName { get; set; }
    }
}
