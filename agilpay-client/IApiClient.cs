using System.Threading.Tasks;
using System.Collections.Generic;
using agilpay.client.models;
using agilpay.models;

namespace agilpay
{
 public interface IApiClient
 {
 System.Threading.Tasks.Task<global::agilpay.client.models.Transaction> AuthorizePayment(global::agilpay.client.models.AuthorizationRequest AuthorizationRequest);
 System.Threading.Tasks.Task<global::agilpay.client.models.Transaction> AuthorizePaymentToken(global::agilpay.client.models.AuthorizationTokenRequest AuthorizationRequest);
 System.Threading.Tasks.Task<System.Collections.Generic.List<global::agilpay.models.CustomerAccount>> GetCustomerTokens(string CustomerID);
 System.Threading.Tasks.Task<global::agilpay.client.models.BalanceResponse> GetBalance(global::agilpay.client.models.BalanceRequest balanceRequest);
 System.Threading.Tasks.Task<bool> IsValidCard(string cardNumber);
 System.Threading.Tasks.Task<bool> IsValidRoutingNumber(string routingNumber);
 System.Threading.Tasks.Task<bool> DeleteCustomerCard(global::agilpay.client.models.DeleteTokenRequest deleteRequest);
 System.Threading.Tasks.Task<global::agilpay.models.CustomerAccount> RegisterToken(global::agilpay.models.RegisterTokenRequest args);
 System.Threading.Tasks.Task<string> CloseBatchResumen(string MerchantKey);
 System.Threading.Tasks.Task<global::agilpay.client.models.Transaction> VoidById(global::agilpay.client.models.VoidByIdRequest args);
 System.Threading.Tasks.Task<global::agilpay.client.models.Transaction> VoidSale(global::agilpay.client.models.VoidSaleRequest args);
 System.Threading.Tasks.Task<global::agilpay.client.models.Transaction> CaptureByID(global::agilpay.client.models.VoidByIdRequest args);
 System.Threading.Tasks.Task<global::agilpay.client.models.Transaction> CaptureAdjustmendByID(global::agilpay.client.models.CaptureAdjustmendByIDRequest args);
 System.Threading.Tasks.Task<global::agilpay.client.models.Transaction> GetTransactionByID(string MerchantKey, string IDTransaction);
 System.Threading.Tasks.Task<global::agilpay.client.models.RecurringScheduleAddResponse> RecurringScheduleAdd(global::agilpay.client.models.RecurringScheduleAddRequest args);
 System.Threading.Tasks.Task<global::agilpay.client.models.RecurringSchedule> RecurringScheduleGet(string MerchantKey, string Service, string CustomerId);
 System.Threading.Tasks.Task<global::agilpay.client.models.RecurringScheduleAddResponse> RecurringScheduleChangeStatus(global::agilpay.client.models.RecurringScheduleChangeStatusRequest args);
 System.Threading.Tasks.Task<global::agilpay.client.models.RecurringScheduleAddResponse> RecurringScheduleUpdate(global::agilpay.client.models.RecurringSchedule args);
 System.Threading.Tasks.Task<global::agilpay.client.models.Transaction> Refund(global::agilpay.client.models.AuthorizationRequest args);
 System.Threading.Tasks.Task<global::agilpay.client.models.Transaction> RefundToken(global::agilpay.client.models.AuthorizationTokenRequest args);
 System.Threading.Tasks.Task<global::agilpay.client.models.Transaction> RefundByID(global::agilpay.client.models.VoidByIdRequest args);
 }
}