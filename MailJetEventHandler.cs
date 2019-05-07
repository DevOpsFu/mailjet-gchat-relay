using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DevOpsFu.AzureFunctions
{
    public static class MailJetEventHandler
    {
        [FunctionName("MailJetEventHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequest req, ILogger log, ExecutionContext context)
        {

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();


            log.LogInformation("C# HTTP trigger function processed a request.");

            log.LogInformation($"GChat destination webhook URI: {config["GChatWebhookURI"]}");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<MailJetEvent>(requestBody);
            log.LogInformation((data.EventType).ToString());

            Uri chatUrl = new Uri(config["GChatWebhookURI"]);


            var message = new GoogleChatEvent();

            if (data.EventType.ToLower() == "sent") 
            {
                message.text = "New GPSystems email form submission sent";
            }
            else 
            {
                message.text = $"A {data.EventType} event occured. Please check the MailJet console for more information";
            }
            
            PostGChatMessage(message, chatUrl);

            return (ActionResult)new OkResult();

        }

        static public void PostGChatMessage(GoogleChatEvent payload, Uri uri)
        {
            string payloadJson = JsonConvert.SerializeObject(payload);

            using (WebClient client = new WebClient())
            {
                var response = client.UploadString(uri.ToString(), "POST", payloadJson);

                //The response text is usually "ok"
                //string responseText = _encoding.GetString(response);
            }
        }
    }

    public class MailJetEvent
    {
        [JsonProperty(PropertyName = "event")]
        public string EventType { get; set; }
        public int time { get; set; }
        public long MessageID { get; set; }
        public string email { get; set; }
        public string Payload { get; set; }
        public string error_related_to { get; set; }
        public string error { get; set; }
    }

    public class GoogleChatEvent
    {
        public string text { get; set; }
    }



}

