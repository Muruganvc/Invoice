using System.Collections.Generic;
using poc.interfaces;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.Security;
using Intuit.Ipp.Core;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Data;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;
using Intuit.Ipp.DataService;
using Microsoft.Extensions.Configuration;

namespace poc.Service
{
    public class InvoceService : IInvoceService
    {
        private readonly IConfiguration _config;
        public InvoceService(IConfiguration config)
        {
            _config = config;
        }
        private (string realmId, string token) HeaderDetail(HttpContext _httpcontext)
        {
            if (_httpcontext.Request.Headers.Count > 0)
            {
                return (realmId: _httpcontext.Request.Headers["realmId"], token: _httpcontext.Request.Headers["token"]);
            }
            return (realmId: "none", token: "none");
        }
        public OAuth2Client Auth2(string clientId, string clientsecret, string redirectUrl, string environment)
        {
            return new OAuth2Client(clientId, clientsecret, redirectUrl, environment);
        }
        public string connectOauth(string clientId, string clientsecret, string redirectUrl, string environment)
        {
            OAuth2Client auth2Client = Auth2(clientId, clientsecret, redirectUrl, environment);
            List<OidcScopes> scopes = new List<OidcScopes>();
            scopes.Add(OidcScopes.Accounting);
            string authorizeUrl = auth2Client.GetAuthorizationURL(scopes).ToString();
            return authorizeUrl;
        }

        public List<CompanyInfo> getCompanyInfo(HttpContext _httpcontext)
        {
            ServiceContext serviceContext = IntializeContext(_httpcontext);
            if (serviceContext == null) return null;
            QueryService<CompanyInfo> querySvc = new QueryService<CompanyInfo>(serviceContext);
            return querySvc.ExecuteIdsQuery("SELECT * FROM CompanyInfo").ToList();
        }
        public List<Invoice> getInvoice(HttpContext _httpcontext)
        {
            ServiceContext serviceContext = IntializeContext(_httpcontext);
            if (serviceContext == null) return null;
            QueryService<Invoice> querySvc = new QueryService<Invoice>(serviceContext);
            return querySvc.ExecuteIdsQuery("SELECT * FROM Invoice").ToList();
        }

        private ServiceContext IntializeContext(HttpContext _httpcontext)
        {
            (string realmId, string token) = HeaderDetail(_httpcontext);
            if (realmId.Equals("none") && token.Equals("none"))
            {
                return null;
            }
            OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(token);
            ServiceContext serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
            //Enable minorversion 
            serviceContext.IppConfiguration.MinorVersion.Qbo = _config.GetSection("ConntectionDetails").GetSection("MinorVersion").Value;
            //Sandbox
            serviceContext.IppConfiguration.BaseUrl.Qbo = _config.GetSection("ConntectionDetails").GetSection("BaseUrl").Value;
            //Enable logging
            serviceContext.IppConfiguration.Logger.RequestLog.EnableRequestResponseLogging = true;
            serviceContext.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = _config.GetSection("ConntectionDetails").GetSection("LogLocation").Value;
            return serviceContext;
        }

        public string InvoicingWorkflow(HttpContext _httpcontext)
        {
            ServiceContext serviceContext = IntializeContext(_httpcontext);
            if (serviceContext == null) return null;
            DataService dataService = new DataService(serviceContext);

            //Create Account 
            Account account = CreateAccount();
            Account accountAdded = dataService.Add<Account>(account);

            //Add customer
            Customer customer = CreateCustomer();
            Customer customerCreated = dataService.Add<Customer>(customer);

            //Add item
            Item item = CreateItem(accountAdded);
            Item itemAdded = dataService.Add<Item>(item);

            //Add Invoice
            Invoice objInvoice = CreateInvoice(customerCreated, itemAdded, _httpcontext);
            Invoice addedInvoice = dataService.Add<Invoice>(objInvoice);

            // sending invoice 
            dataService.SendEmail<Invoice>(addedInvoice, "vcmuruganmca@gmail.com");

            //Recieve payment for this invoice
            Payment payment = CreatePayment(customerCreated, addedInvoice);
            dataService.Add<Payment>(payment);

            return "Created";
        }

        private Account CreateAccount()
        {
            Random randomNum = new Random();
            Account account = new Account();
            account.Name = "Name_" + randomNum.Next();
            account.FullyQualifiedName = account.Name;
            account.Classification = AccountClassificationEnum.Revenue;
            account.ClassificationSpecified = true;
            account.AccountType = AccountTypeEnum.Bank;
            account.AccountTypeSpecified = true;
            account.CurrencyRef = new ReferenceType()
            {
                name = "Mahindra xuv 300 w8 optional top end model",
                Value = "USD"
            };
            return account;
        }
        private Item CreateItem(Account incomeAccount)
        {
            Item item = new Item();
            Random randomNum = new Random();
            item.Name = "Mahindra xuv 300 w8 optional top end model-" + randomNum.Next();
            item.Description = "xuv 3oo w8 optional top end model";
            item.Type = ItemTypeEnum.NonInventory;
            item.TypeSpecified = true;
            item.Active = true;
            item.ActiveSpecified = true;
            item.Taxable = false;
            item.TaxableSpecified = true;
            item.UnitPrice = new Decimal(1000.00);
            item.UnitPriceSpecified = true;
            item.TrackQtyOnHand = false;
            item.TrackQtyOnHandSpecified = true;
            item.IncomeAccountRef = new ReferenceType()
            {
                name = incomeAccount.Name,
                Value = incomeAccount.Id
            };
            item.ExpenseAccountRef = new ReferenceType()
            {
                name = incomeAccount.Name,
                Value = incomeAccount.Id
            };
            return item;
        }
        private Customer CreateCustomer()
        {
            Random random = new Random();
            Customer customer = new Customer();
            customer.GivenName = "Indian" + random.Next();
            customer.FamilyName = "Indian";
            customer.DisplayName = customer.CompanyName;
            return customer;
        }
        private Invoice CreateInvoice(Customer customer, Item item, HttpContext _httpcontext)
        {
            ServiceContext serviceContext = IntializeContext(_httpcontext);
            Invoice invoice = new Invoice();
            invoice.CustomerRef = new ReferenceType()
            {
                Value = customer.Id
            };
            List<Line> lineList = new List<Line>();
            Line line = new Line();
            line.Description = "Mahindra xuv 300 w8 optional top end model";
            line.Amount = new Decimal(1000.00);
            line.AmountSpecified = true;

            SalesItemLineDetail salesItemLineDetail = new SalesItemLineDetail();
            salesItemLineDetail.Qty = new Decimal(1.0);
            salesItemLineDetail.ItemRef = new ReferenceType()
            {
                Value = item.Id
            };
            line.AnyIntuitObject = salesItemLineDetail;
            line.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
            line.DetailTypeSpecified = true;
            lineList.Add(line);
            invoice.Line = lineList.ToArray();
            invoice.DueDate = DateTime.UtcNow.Date;
            invoice.DueDateSpecified = true;
            invoice.TotalAmt = new Decimal(1000.00);
            invoice.TotalAmtSpecified = true;
            invoice.EmailStatus = EmailStatusEnum.NotSet;
            invoice.EmailStatusSpecified = true;
            invoice.Balance = new Decimal(1000.00);
            invoice.BalanceSpecified = true;
            invoice.TxnDate = DateTime.UtcNow.Date;
            invoice.TxnDateSpecified = true;
            invoice.TxnTaxDetail = new TxnTaxDetail()
            {
                TotalTax = Convert.ToDecimal(10),
                TotalTaxSpecified = true,
            };
            return invoice;
        }
        private Payment CreatePayment(Customer customer, Invoice invoiceCreated)
        {
            Payment payment = new Payment();
            payment.CustomerRef = new ReferenceType
            {
                name = customer.DisplayName,
                Value = customer.Id
            };
            payment.CurrencyRef = new ReferenceType
            {
                type = "Mahindra xuv 300 w8 optional top end model",
                Value = "USD"
            };
            payment.TotalAmt = invoiceCreated.TotalAmt;
            payment.TotalAmtSpecified = true;
            List<LinkedTxn> linkedTxns = new List<LinkedTxn>();
            linkedTxns.Add(new LinkedTxn()
            {
                TxnId = invoiceCreated.Id,
                TxnType = TxnTypeEnum.Invoice.ToString()
            });
            foreach (Line line in invoiceCreated.Line)
            {
                line.LinkedTxn = linkedTxns.ToArray();
            }
            payment.Line = invoiceCreated.Line;
            return payment;
        }
    }
}