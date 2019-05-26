namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.HpcAcm.Common.Dto;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
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

            public HttpStatusCode StatusCode { get; private set; }

            public ApiError(HttpResponseMessage msg) : base(ErrorMessage(msg))
            {
                StatusCode = msg.StatusCode;
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
            var result = JsonConvert.DeserializeObject<IEnumerable<Group>>(body);
            return result;
        }

        public async T.Task<Group> GetGroupAsync(int groupId)
        {
            var response = await client.GetAsync($"/api/node-groups/{groupId}");
            EnsureSuccess(response);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Group>(body);
            return result;
        }

        public async T.Task<Group> CreateGroupAsync(Group group)
        {
            var content = new StringContent(JsonConvert.SerializeObject(group));
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
            var response = await client.PostAsync("/api/node-groups", content);
            EnsureSuccess(response);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Group>(body);
            return result;
        }

        public async T.Task<Group> UpdateGroupAsync(Group group)
        {
            var content = new StringContent(JsonConvert.SerializeObject(group));
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
            var response = await client.PutAsync($"/api/node-groups/{group.Id}", content);
            EnsureSuccess(response);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Group>(body);
            return result;
        }

        public async T.Task DeleteGroupAsync(int groupId)
        {
            var response = await client.DeleteAsync($"/api/node-groups/{groupId}");
            EnsureSuccess(response);
        }

        public async T.Task<IEnumerable<string>> GetNodesOfGroupAsync(int groupId)
        {
            var response = await client.GetAsync($"/api/node-groups/{groupId}/nodes");
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<IEnumerable<string>>(body);
            return result;
        }

        public async T.Task<IEnumerable<string>> AddNodesToGroupAsync(int groupId, IEnumerable<string> nodeNames)
        {
            var content = new StringContent(JsonConvert.SerializeObject(nodeNames));
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
            var response = await client.PostAsync($"/api/node-groups/{groupId}/nodes", content);
            EnsureSuccess(response);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<IEnumerable<string>>(body);
            return result;
        }

        public async T.Task<IEnumerable<string>> RemoveNodesFromGroupAsync(int groupId, IEnumerable<string> nodeNames)
        {
            var content = new StringContent(JsonConvert.SerializeObject(nodeNames));
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
            var response = await client.PostAsync($"/api/node-groups/{groupId}/nodes/delete", content);
            EnsureSuccess(response);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<IEnumerable<string>>(body);
            return result;
        }
    }
}
