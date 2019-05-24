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

        private List<GroupWithNodes> groups { get => provider.Groups; }

        private int nextId { get => provider.NextId; set => provider.NextId = value; }

        public GroupsV1Controller(DataProvider provider)
        {
            this.provider = provider;
        }

        [HttpGet()]
        public ActionResult GetGroups()
        {
            return new OkObjectResult(groups);
        }

        [HttpGet]
        [Route("{id}")]
        public ActionResult GetGroup(int id)
        {
            var idx = groups.FindIndex(g => g.Id == id);
            if (idx < 0)
            {
                return NotFound($"Group {id} is not found!");
            }
            return new OkObjectResult(groups[idx]);
        }

        [HttpPost]
        public ActionResult CreateGroup([FromBody] Group group)
        {
            if (string.IsNullOrWhiteSpace(group.Name))
            {
                return BadRequest("Group name must not be blank!");
            }
            if (groups.Any(g => g.Name == group.Name))
            {
                return BadRequest("Duplicate group name!");
            }
            var newGroup = new GroupWithNodes() { Id = nextId++, Name = group.Name, Description = group.Description, Managed = false };
            groups.Add(newGroup);
            return new OkObjectResult(newGroup.ToGroup());
        }

        [HttpPut] 
        [Route("{id}")]
        public ActionResult UpdateGroup(int id, [FromBody] Group update)
        {
            var idx = groups.FindIndex(g => g.Id == id);
            if (idx < 0)
            {
                return NotFound($"Group {id} is not found!");
            }
            var group = groups[idx];
            if (group.Managed)
            {
                return BadRequest("Managed group can't be changed!");
            }
            if (string.IsNullOrWhiteSpace(update.Name))
            {
                return BadRequest("Group name must not be blank!");
            }
            if (groups.Any(g => g.Name == update.Name && g.Id != id))
            {
                return BadRequest($"Duplicate group name {update.Name}!");
            }
            group.Name = update.Name;
            group.Description = update.Description;
            return new OkObjectResult(group.ToGroup());
        }

        [HttpDelete]
        [Route("{id}")]
        public ActionResult DeleteGroup(int id)
        {
            var idx = groups.FindIndex(g => g.Id == id);
            if (idx < 0)
            {
                return NotFound($"Group {id} is not found!");
            }
            groups.RemoveAt(idx);
            return Ok();
        }

        [HttpPost]
        [Route("{id}/nodes")]
        public ActionResult AddNodesToGroup(int id, [FromBody] string[] nodeNames)
        {
            var idx = groups.FindIndex(g => g.Id == id);
            if (idx < 0)
            {
                return NotFound($"Group {id} is not found!");
            }
            var group = groups[idx];
            group.Nodes.UnionWith(nodeNames);
            return new OkObjectResult(group.Nodes.ToList());
        }

        [HttpDelete]
        [Route("{id}/nodes")]
        public ActionResult RemoveNodesFromGroup(int id, [FromBody] string[] nodeNames)
        {
            var idx = groups.FindIndex(g => g.Id == id);
            if (idx < 0)
            {
                return NotFound($"Group {id} is not found!");
            }
            var group = groups[idx];
            group.Nodes.ExceptWith(nodeNames);
            return new OkObjectResult(group.Nodes.ToList());
        }
    }
}
