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
            var result = await this.provider.GetNodeGroupAsync(id, token);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPost]
        public async T.Task<ActionResult> CreateGroupAsync([FromBody] Group group, CancellationToken token)
        {
            Group result;
            try
            {
                result = await this.provider.CreateNodeGroupAsync(group, token);
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
            return Ok(result);
        }

        [HttpPut]
        [Route("{id}")]
        public async T.Task<ActionResult> UpdateGroupAsync(int id, [FromBody] Group group, CancellationToken token)
        {
            Group result;
            try
            {
                result = await provider.UpdateNodeGroupAsync(new Group() { Id = id, Name = group.Name, Description = group.Description }, token);
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
            return Ok(result);
        }

        [HttpDelete]
        [Route("{id}")]
        public async T.Task<ActionResult> DeleteGroupAsync(int id, CancellationToken token)
        {
            try
            {
                await provider.DeleteNodeGroupAsync(new Group() { Id = id }, token);
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
            return Ok();
        }

        [HttpPost]
        [Route("{id}/nodes")]
        public async T.Task<ActionResult> AddNodesToGroupAsync(int id, [FromBody] string[] nodeNames, CancellationToken token)
        {
            GroupWithNodes result;
            try
            {
                result = await provider.AddNodesToGroup(new GroupWithNodes() { Id = id, Nodes = nodeNames }, token);
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
            return Ok(result.Nodes);
        }

        [HttpDelete]
        [Route("{id}/nodes")]
        public async T.Task<ActionResult> RemoveNodesFromGroupAsync(int id, [FromBody] string[] nodeNames, CancellationToken token)
        {
            GroupWithNodes result;
            try
            {
                result = await provider.RemoveNodesFromGroup(new GroupWithNodes() { Id = id, Nodes = nodeNames }, token);
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
            return Ok(result.Nodes);
        }
    }
}
