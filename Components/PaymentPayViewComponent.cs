using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace NopFarsi.Payment.SizPay.Components
{
    [ViewComponent(Name = "PaymentPay")]
    public class PaymentPayViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/NopFarsi.Payment.SizPay/Views/PaymentInfo.cshtml");
        }
    }
}