namespace agilpay.client.models
{
    public class Invoice
    {
        public string Number { get; set; }
        public string Date { get; set; }
        public string Total_Amount { get; set; }
        public decimal Min_Amount { get; set; }
        public decimal Max_Amount { get; set; }
        public string Description { get; set; }
    }
}
