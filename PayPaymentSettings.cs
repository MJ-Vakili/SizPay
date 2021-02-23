using Nop.Core.Configuration;

namespace NopFarsi.Payment.SizPay
{
    /// <summary>
    /// Represents settings of the Pay Standard payment plugin
    /// </summary>
    public class PayPaymentSettings : ISettings
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string MerchentId { get; internal set; }
        public string TerminalId { get; internal set; }

        public string DocDate { get; set; }

        public string ReturnURL { get; set; }

        public bool IsToman { get; set; }

        //public string RedirectUrl => $"{Redirect}/PaymentPay/VerifyPayment";
    }
}