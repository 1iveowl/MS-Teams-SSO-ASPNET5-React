using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace teams_sso_sample.Controllers
{
    [ApiController]
    public class GetGraphAccessTokenController : ControllerBase
    {
        [HttpGet]
        [Route("api/getGraphAccessToken")]
        public async Task<IActionResult> Get(string token)
        {
            return new OkResult();
        }
    }
}
