namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using T = System.Threading.Tasks;

    [Route("v1/groups")]
    public class GroupsV1Controller : Controller
    {
        private readonly DataProvider provider;

        public GroupsV1Controller(DataProvider provider)
        {
            this.provider = provider;
        }

        [HttpGet()]
        public async T.Task<ActionResult> GetGroupsAsync(CancellationToken token)
        {
            return Ok(await this.provider.GetNodeGroupsAsync(token));
        }

        [HttpGet]
        [Route("{id}")]
        public async T.Task<ActionResult> GetGroupAsync(int id, CancellationToken token)
        {
            return Ok(await this.provider.GetNodeGroupAsync(id, token));
        }

        [HttpPost]
        public ActionResult CreateGroup([FromBody] Group group)
        {
            return new OkObjectResult(new Group());
        }

        [HttpPut]
        [Route("{id}")]
        public ActionResult UpdateGroup(int id, [FromBody] Group group)
        {
            return new OkObjectResult(new Group());
        }

        [HttpDelete]
        [Route("{id}")]
        public void DeleteGroup(int id)
        {

        }

        [HttpPost]
        [Route("{id}/nodes")]
        public ActionResult AddNodesToGroup(int id, [FromBody] string[] nodeNames)
        {
            return new OkObjectResult(new string[] { });
        }

        [HttpDelete]
        [Route("{id}/nodes")]
        public ActionResult RemoveNodesFromGroup(int id, [FromBody] string[] nodeNames)
        {
            return new OkObjectResult(new string[] { });
        }
    }
}
