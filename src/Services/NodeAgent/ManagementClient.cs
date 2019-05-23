namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.HpcAcm.Common.Dto;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using T = System.Threading.Tasks;

    public class ManagementClient
    {
        public class ApiError : Exception
        {
            static string ErrorMessage(HttpResponseMessage response)
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return $"HTTP code: {response.StatusCode} message: {body}";
            }

            public ApiError(HttpResponseMessage msg) : base(ErrorMessage(msg))
            {
            }
        }

        private HttpClient client;

        public ManagementClient()
        {
            client = new HttpClient()
            {
                BaseAddress = new Uri("http://localhost:5832"),
            };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private void EnsureSuccess(HttpResponseMessage msg)
        {
            if (!msg.IsSuccessStatusCode)
            {
                throw new ApiError(msg);
            }
        }

        public async T.Task<IEnumerable<Group>> GetGroupsAsync()
        {
            var response = await client.GetAsync("/api/node-groups");
            EnsureSuccess(response);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<IEnumerable<GroupWithNodes>>(body);
            return result;
        }

        public async T.Task<Group> CreateGroupAsync(Group group)
        {
            return null;
        }

        public async T.Task<Group> UpdateGroupAsync(Group group)
        {
            return null;
        }

        public async T.Task DeleteGroupAsync(int groupId)
        {

        }

        public async T.Task<IEnumerable<string>> GetNodesOfGroupAsync(int groupId)
        {
            var response = await client.GetAsync($"/api/node-groups/{groupId}/nodes");
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<IEnumerable<string>>(body);
            return result;
        }

        public async T.Task<IEnumerable<string>> AddNodesToGroupAsync(int groupId, string[] nodeNames)
        {
            return null;
        }

        public async T.Task<IEnumerable<string>> RemoveNodesFromGroupAsync(int groupId, string[] nodeNames)
        {
            return null;
        }
    }
}
