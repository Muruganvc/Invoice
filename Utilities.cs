
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using poc.interfaces;

namespace poc
{
    public enum InvoiceEnum
    {
        invoice,
        companyInfo
    }
        public static class Utilities
    {
        public static (string realmId, string code, string state, string error) QueryStringValue(this HttpContext context)
        {
            string realmId = string.Empty, code = string.Empty, state = string.Empty, error = string.Empty;
            if (!String.IsNullOrEmpty(context.Request.Query["code"]))
                code = context.Request.Query["code"];

            if (!String.IsNullOrEmpty(context.Request.Query["state"]))
                state = context.Request.Query["state"];

            if (!String.IsNullOrEmpty(context.Request.Query["realmId"]))
                realmId = context.Request.Query["realmId"];

            if (!String.IsNullOrEmpty(context.Request.Query["error"]))
                error = context.Request.Query["error"];

            return (realmId, code, state, error);
        }

        public static (string clientId, string clientsecret, string redirectUrl, string environment) AuthKeyValues(this IInvoceService sevice, IConfiguration config)
        {
            string clientId = string.Empty, clientsecret = string.Empty, redirectUrl = string.Empty, environment = string.Empty;
            clientId = config.GetSection("Authentication").GetSection("clientId").Value;
            clientsecret = config.GetSection("Authentication").GetSection("clientsecret").Value;
            redirectUrl = config.GetSection("Authentication").GetSection("redirectUrl").Value;
            environment = config.GetSection("Authentication").GetSection("environment").Value;
            return (clientId, clientsecret, redirectUrl, environment);
        }
    }
}