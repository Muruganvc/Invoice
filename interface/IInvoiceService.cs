using System.Collections.Generic;
using Intuit.Ipp.Data;
using Intuit.Ipp.OAuth2PlatformClient;
using Microsoft.AspNetCore.Http;

namespace poc.interfaces
{
    public interface IInvoceService
    {
        string connectOauth(string clientId, string clientsecret, string redirectUrl, string environment);
        OAuth2Client Auth2(string clientId, string clientsecret, string redirectUrl, string environment);
        List<CompanyInfo> getCompanyInfo(HttpContext _httpcontext);
        List<Invoice> getInvoice(HttpContext _httpcontext);
        string InvoicingWorkflow(HttpContext _httpcontext);
    }
}