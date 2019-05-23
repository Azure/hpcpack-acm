namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Route("v1/groups")]
    public class GroupsV1Controller : Controller
    {
        private readonly DataProvider provider;

        public GroupsV1Controller(DataProvider provider)
        {
            this.provider = provider;
        }

        [HttpGet()]
        public ActionResult GetGroups()
        {
            return new OkObjectResult(new Group[] { });
        }

        [HttpPost]
        public ActionResult CreateGroup([FromBody] Group group)
        {
            return new OkObjectResult(new Group());
        }

        [HttpPut] 
        [Route("{name}")]
        public ActionResult UpdateGroup(string name, [FromBody] Group group)
        {
            return new OkObjectResult(new Group());
        }

        [HttpDelete]
        [Route("{name}")]
        public void DeleteGroup(string name)
        {

        }

        [HttpGet]
        [Route("{name}/nodes")]
        public ActionResult GetNodesOfGroup(string name)
        {
            return new OkObjectResult(new string[] { });
        }

        [HttpPost]
        [Route("{name}/nodes")]
        public ActionResult AddNodesToGroup(string name, [FromBody] string[] nodeNames)
        {
            return new OkObjectResult(new string[] { });
        }

        [HttpDelete]
        [Route("{name}/nodes")]
        public ActionResult RemoveNodesFromGroup(string name, [FromBody] string[] nodeNames)
        {
            return new OkObjectResult(new string[] { });
        }
    }
}
