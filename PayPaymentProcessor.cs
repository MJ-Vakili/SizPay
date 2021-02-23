using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Tax;

namespace NopFarsi.Payment.SizPay
{
    /// <summary>
    /// PayStandard payment processor
    /// </summary>
    public class PayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly IPaymentService paymentService;
        private readonly PayPaymentSettings _payPaymentSettings;
        private readonly ILogger logger;

        #endregion Fields

        #region Ctor

        public PayPaymentProcessor(CurrencySettings currencySettings,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ISettingService settingService,
            ITaxService taxService,
            IWebHelper webHelper,
            PayPaymentSettings payPaymentSettings,
            ILogger logger,
            IPaymentService paymentService)
        {
            this._currencySettings = currencySettings;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._currencyService = currencyService;
            this._genericAttributeService = genericAttributeService;
            this._httpContextAccessor = httpContextAccessor;
            this._localizationService = localizationService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._settingService = settingService;
            this._taxService = taxService;
            this._webHelper = webHelper;
            this._payPaymentSettings = payPaymentSettings;
            this.logger = logger;
            this.paymentService = paymentService;
        }

        #endregion Ctor

        #region Utilities

        /// <summary>
        /// Gets Pay URL
        /// </summary>
        /// <returns></returns>
        private string GetPayUrl()
        {
            return "https://sizpay.ir/payment/send";
        }

        /// <summary>
        /// Gets IPN Pay URL
        /// </summary>
        /// <returns></returns>
        private string GetIpnPayUrl()
        {
            return "https://sizpay.ir/payment/verify";
        }

        #endregion Utilities

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult();
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //create common query parameters for the request
            try
            {
                ServicePointManager.ServerCertificateValidationCallback =
                               delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
                var kimial = new KimialPG.KimiaIPGRouteServiceSoapClient(KimialPG.KimiaIPGRouteServiceSoapClient.EndpointConfiguration.KimiaIPGRouteServiceSoap);
                decimal OrderTotal = postProcessPaymentRequest.Order.OrderTotal;
                if (!_payPaymentSettings.IsToman)
                    OrderTotal = postProcessPaymentRequest.Order.OrderTotal * 10;
                string returnUrl = this._webHelper.GetStoreLocation() + "SizPay/Verify?OrderId=" + postProcessPaymentRequest.Order.Id.ToString();

                DateTime d = DateTime.Now;
                PersianCalendar pc = new PersianCalendar();

                var result1 = kimial.GetTokenAsync(new KimialPG.GenerateToken()
                {
                    UserName = _payPaymentSettings.UserName,
                    Password = _payPaymentSettings.Password,
                    MerchantID = _payPaymentSettings.MerchentId,
                    TerminalID = _payPaymentSettings.TerminalId,
                    Amount = Convert.ToInt32(OrderTotal),
                    DocDate = string.Format("{0}/{1}/{2}", pc.GetYear(d), pc.GetMonth(d), pc.GetDayOfMonth(d)),
                    OrderID = postProcessPaymentRequest.Order.Id.ToString(),
                    ReturnURL = returnUrl,
                    //ExtraInf=_payPaymentSettings.InvoiceNo,
                    InvoiceNo = postProcessPaymentRequest.Order.Id.ToString(),
                    AppExtraInf = new KimialPG.AppExtraInf(),
                }).Result;//.Body.GetTokenResult;
                var result2 = result1.Body;
                var result = result2.GetTokenResult;
                if (result.ResCod == 0)
                {
                    postProcessPaymentRequest.Order.AuthorizationTransactionCode = result.Token;
                    EngineContext.Current.Resolve<IOrderService>().UpdateOrder(postProcessPaymentRequest.Order);

                    this._httpContextAccessor.HttpContext.Response.Redirect(
                        this._webHelper.GetStoreLocation() + "SizPay/Pay?MerchantID=" + _payPaymentSettings.MerchentId
                        + "&TerminalID=" + _payPaymentSettings.TerminalId +
                        "&Token=" + result.Token);
                }
                else
                {
                    this.logger.Error("Error Code : " + result.ResCod + " " + "Error Message : " + result.Message);
                    throw new NopException("Error Code : " + result.ResCod + " " + "Error Message : " + result.Message);
                }
            }
            catch (Exception exp)
            {
                this.logger.Error("Error Code : " + exp.Message);
                throw new NopException("Error Message" + exp.Message);
            }
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return this.paymentService.CalculateAdditionalFee(cart, 0, false);
            //return this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
            // _payPaymentSettings.AdditionalFee, _payPaymentSettings.AdditionalFeePercentage);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            return new RefundPaymentResult { Errors = new[] { "Refund method not supported" } };
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            if (this.PluginDescriptor.Author != "nopFarsi.ir")
                throw new Exception();
            return $"{_webHelper.GetStoreLocation()}Admin/SizPay/Configure";
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return "PaymentPay";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            base.Install();
            this._localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Pay.Fields.UserName", "UserName");
            this._localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Pay.Fields.Password", "Password");
            this._localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Pay.Fields.MerchantID", "MerchantID");
            this._localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Pay.Fields.TerminalID", "TerminalID");
            this._localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Pay.Fields.IsToman", "IsToman");
            this._localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.SizPay.Fields.RedirectionTip", "به درگاه سیزپی منتقل می شوید.");
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<PayPaymentSettings>();

            //locales

            _localizationService.DeletePluginLocaleResource("plugins.payments.pay.instructions");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.Fields.Api");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.Fields.Api.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.Fields.Redirect");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.Fields.Redirect.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.Fields.AdditionalFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.Fields.AdditionalFee.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.Fields.AdditionalFeePercentage");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.Fields.AdditionalFeePercentage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.Fields.RedirectionTip");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.PaymentMethodDescription");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Pay.RoundingWarning");

            base.Uninstall();
        }

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentPay";
        }

        #endregion Methods

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to Pay site to complete the payment"
            get { return _localizationService.GetResource("Plugins.Payments.Pay.PaymentMethodDescription"); }
        }

        #endregion Properties
    }
}