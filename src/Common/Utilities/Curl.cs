namespace Microsoft.HpcAcm.Common.Utilities
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Curl
    {
        public static Task<T> PostAsync<T, TBody>(string requestUri, Dictionary<string, string> headers, TBody body, CancellationToken token)
        {
            StringContent content;
            if (body == null)
            {
                content = new StringContent("");
            }
            else
            {
                content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            }

            return RequestAsync<T>(requestUri, headers, (c, uri, t) => c.PostAsync(uri, content, t), token); 
        }

        public static Task<T> GetAsync<T>(string requestUri, Dictionary<string, string> headers, CancellationToken token)
        {
            return RequestAsync<T>(requestUri, headers, (c, uri, t) => c.GetAsync(uri, t), token); 
        }

        public static async Task<T> RequestAsync<T>(string requestUri, Dictionary<string, string> headers, Func<HttpClient, string, CancellationToken, Task<HttpResponseMessage>> requester, CancellationToken token)
        {
            using (HttpClient client = new HttpClient())
            {
                foreach (var h in headers)
                {
                    client.DefaultRequestHeaders.Add(h.Key, h.Value);
                }

                var res = await requester(client, requestUri, token);
                res.EnsureSuccessStatusCode();
                var result = await res.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(result);
            }
        }
    }
}
