namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class GroupWithNodes : Group
    {
        public HashSet<string> Nodes { get; set; } = new HashSet<string>();

        public Group ToGroup()
        {
            return new Group() { Name = Name, Description = Description, Managed = Managed };
        }
    }

    [Route("v1/groups")]
    public class GroupsV1Controller : Controller
    {
        private readonly DataProvider provider;

        private List<GroupWithNodes> groups = new List<GroupWithNodes>();

        public GroupsV1Controller(DataProvider provider)
        {
            this.provider = provider;
            groups.Add(new GroupWithNodes() { Name = "HeadNodes", Description = "The head nodes in the cluster", Managed = true });
            groups.Add(new GroupWithNodes() { Name = "ComputeNodes", Description = "The compute nodes in the cluster", Managed = true });
            groups.Add(new GroupWithNodes() { Name = "LinuxNodes", Description = "The linux nodes in the cluster", Managed = true });
        }

        [HttpGet()]
        public ActionResult GetGroups()
        {
            return new OkObjectResult(groups.Select(g => g.ToGroup()));
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
            var newGroup = new GroupWithNodes() { Name = group.Name, Description = group.Description, Managed = false };
            groups.Add(newGroup);
            return new OkObjectResult(newGroup.ToGroup());
        }

        [HttpPut] 
        [Route("{name}")]
        public ActionResult UpdateGroup(string name, [FromBody] Group update)
        {
            var idx = groups.FindIndex(g => g.Name == name);
            if (idx < 0)
            {
                return NotFound($"Group {name} is not found!");
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
            update.Managed = false;
            group.Name = update.Name;
            group.Description = update.Description;
            return new OkObjectResult(group.ToGroup());
        }

        [HttpDelete]
        [Route("{name}")]
        public ActionResult DeleteGroup(string name)
        {
            var idx = groups.FindIndex(g => g.Name == name);
            if (idx < 0)
            {
                return NotFound($"Group {name} is not found!");
            }
            groups.RemoveAt(idx);
            return Ok();
        }

        [HttpGet]
        [Route("{name}/nodes")]
        public ActionResult GetNodesOfGroup(string name)
        {
            var idx = groups.FindIndex(g => g.Name == name);
            if (idx < 0)
            {
                return NotFound($"Group {name} is not found!");
            }
            return new OkObjectResult(groups[idx].Nodes.ToList());
        }

        [HttpPost]
        [Route("{name}/nodes")]
        public ActionResult AddNodesToGroup(string name, [FromBody] string[] nodeNames)
        {
            var idx = groups.FindIndex(g => g.Name == name);
            if (idx < 0)
            {
                return NotFound($"Group {name} is not found!");
            }
            var group = groups[idx];
            group.Nodes.UnionWith(nodeNames);
            return new OkObjectResult(group.Nodes.ToList());
        }

        [HttpDelete]
        [Route("{name}/nodes")]
        public ActionResult RemoveNodesFromGroup(string name, [FromBody] string[] nodeNames)
        {
            var idx = groups.FindIndex(g => g.Name == name);
            if (idx < 0)
            {
                return NotFound($"Group {name} is not found!");
            }
            var group = groups[idx];
            group.Nodes.ExceptWith(nodeNames);
            return new OkObjectResult(group.Nodes.ToList());
        }
    }
}
