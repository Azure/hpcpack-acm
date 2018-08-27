using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.HpcAcm.Frontend.Controllers
{
    [Authorize]
    [Route("v1/validation")]
    public class ValidationV1Controller : Controller
    {
        [HttpGet()]
        public IActionResult Validate()
        {
            return NoContent();
        }
    }
}