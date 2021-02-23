using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class ApiCallBack
{
    public int status { get; set; }
    public string transId { get; set; }
    public double amount { get; set; }
    public string errorCode { get; set; }
    public string errorMessage { get; set; }

    public ApiCallBack()
    {
    }
}