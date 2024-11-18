using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagSharpLib.HttpClientHelper
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using EventBroadcasting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using RagSharpLib.EventHandler;

    internal static class HttpHelper
    {
        /// <summary>
        /// Sends a generic HTTP POST request to the specified endpoint.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request payload.</typeparam>
        /// <typeparam name="TResponse">The expected type of the response.</typeparam>
        /// <param name="endpoint">The URI of the endpoint.</param>
        /// <param name="payload">The request payload.</param>
        /// <returns>The response from the endpoint deserialized into the specified type.</returns>
        private static readonly HttpClient httpClient = new HttpClient();
        public static async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload, string bearerToken = null, bool logModelEvent = false)
        {

            if (string.IsNullOrEmpty(bearerToken))
            {
                throw new ArgumentNullException("API key is requierd to use RagSharp");
            }
            // Set authentication header if token is provided
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            var jsonPayload = JsonConvert.SerializeObject(payload, settings);
            if (logModelEvent)
            {
                EventAggregator.Publish(new LLMEvent() { Data = JObject.Parse(jsonPayload), EventType = EventType.LLMCallEvent });
            }
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TResponse>(responseContent);
                if (logModelEvent)
                {
                    EventAggregator.Publish(new LLMEvent() { Data = JObject.Parse(responseContent), EventType = EventType.LLMResponseEvent });
                }
                return result;
            }
            catch (Exception ex)
            {
                if (response != null && !response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception(error);
                }
                throw;
            }
        }
    }

}
