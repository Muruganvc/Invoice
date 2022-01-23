
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using poc.interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Owin;
using Microsoft.Extensions.Configuration;

namespace poc.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class InvoiceController : BaseApiController
    {
        private readonly IInvoceService _service;
        private readonly IOwinContext _OwinContext;
        private readonly IConfiguration _config;
        public InvoiceController(IInvoceService service, IOwinContext OwinContext, IConfiguration config) : base(service, config)
        {
            _service = service;
            _OwinContext = OwinContext;
            _config = config;
        }

        [HttpGet("connectOauth")]
        public IActionResult ConnectOauth()
        {
            return Redirect(_service.connectOauth(clientId, clientsecret, redirectUrl, environment));
        }
        [HttpGet("validation")]
        public async Task<IActionResult> Validation()
        {
            (string realmId, string code, string state, string error) = HttpContext.QueryStringValue();
            var claim = await base.GetAuthTokensAsync(code, realmId, _OwinContext);
            return Ok(claim);
        }
        [HttpGet("getInvoiceDetails/{type}")]
        public IActionResult getInvoiceDetails(InvoiceEnum type)
        {
            if (type == InvoiceEnum.invoice)
            {
                return Ok(this._service.getInvoice(HttpContext));
            }
            else if (type == InvoiceEnum.companyInfo)
            {
                return Ok(this._service.getCompanyInfo(HttpContext));
            }
            return BadRequest();
        }
        [HttpPost("createInvoice")]
        public IActionResult createInvoice()
        {
            return Ok(this._service.InvoicingWorkflow(HttpContext));
        }
    }
}
