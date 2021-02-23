using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace NopFarsi.Payment.SizPay
{
    public class Payment
    {
        public string Verify(string transId, string api)
        {
            var client = new HttpClient();
            var requestContent = new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("api", api),
                        new KeyValuePair<string, string>("transId", transId),
                    });
            HttpResponseMessage response = client.PostAsync(
                "https://sizpay.ir/payment/verify",
                requestContent).Result;
            HttpContent responseContent = response.Content;

            var jsonString = responseContent.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<ApiCallBack>(jsonString);
            if (result.status == 1)
            {
                return jsonString;
            }
            else
            {
                return jsonString;
            }
        }

        public async Task<string> Pay(decimal amount, int id, string api, string redirect)
        {
            var client = new HttpClient();
            var requestContent = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("api", api),
                new KeyValuePair<string, string>("amount", ((long)amount).ToString("")),
                new KeyValuePair<string, string>("redirect", redirect),
                new KeyValuePair<string, string>("factorNumber", $"{id}"),
            });
            try
            {
                HttpResponseMessage response = await client.PostAsync(
                    "https://sizpay.ir/payment/send", requestContent);
                HttpContent responseContent = response.Content;
                return await responseContent.ReadAsStringAsync();
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }
    }
}