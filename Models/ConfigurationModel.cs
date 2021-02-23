using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopFarsi.Payment.SizPay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Pay.Fields.UserName")]
        public string UserName { get; set; }

        public bool UserName_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Pay.Fields.Password")]
        public string Password { get; set; }

        public bool Password_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Pay.Fields.MerchantID")]
        public string MerchantID { get; set; }

        public bool MerchantID_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Pay.Fields.TerminalID")]
        public string TerminalID { get; set; }

        public bool TerminalID_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Pay.Fields.IsToman")]
        public bool IsToman { get; set; }

        public bool IsToman_OverrideForStore { get; set; }
    }
}