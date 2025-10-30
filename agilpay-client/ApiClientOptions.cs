namespace agilpay
{
 public class ApiClientOptions
 {
 public string BaseUrl { get; set; }
 public string ClientId { get; set; }
 public string ClientSecret { get; set; }
 // Optional timeout in seconds for HttpClient
 public int? TimeoutSeconds { get; set; }
 }
}