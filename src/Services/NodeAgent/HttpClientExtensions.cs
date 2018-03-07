namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, Uri uri, T obj, Dictionary<string, string> headers, CancellationToken token)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            foreach (var h in headers)
            {
                request.Headers.Add(h.Key, h.Value);
            }

            request.Content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");

            return await client.SendAsync(request, token);
        }
    }
}