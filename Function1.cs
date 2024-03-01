using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Text.Json.Serialization;
using System.Linq;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace AzFunction
{
    public class Function1
    {
        [FunctionName("Function1")]
        public async Task RunAsync([BlobTrigger("input-file-container/{name}", Connection = "")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n ");
            using (StreamReader reader = new StreamReader(myBlob))
            {

                var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

                // Retrieve settings
                string connectionString = config["AzureWebJobsStorage"];

                string inputStr = await reader.ReadToEndAsync();
                //var inputData = JsonConvert.DeserializeObject<InputData>(inputStr);
                HttpClient _httpClient = new HttpClient();
                //_httpClient.DefaultRequestHeaders.Add("subscriptionId", inputData.subscriptionId);
                //_httpClient.DefaultRequestHeaders.Add("resourceGroup", inputData.resourceGroup);
                //_httpClient.DefaultRequestHeaders.Add("azureLoadTestingResourceName", inputData.azureLoadTestingResourceName);

                //for (int i = 0; i<inputData.loadTestRuns.ToList().Count; i++)
                //{
                    await CreateSeqLoadTests(inputStr, _httpClient);
                //}
                // Parse JSON
                //var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

                // Access JSON properties
                //var propertyValue = data["azureLoadTestingResourceName"];
                //log.LogInformation($"{propertyValue}");
            }

        }
        private async Task CreateSeqLoadTests(string jsonString, HttpClient _httpClient)
        {
           
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;
            string apiUrl = $"https://localhost:44394/api/RunLoadTest";
            //var reqContent = JsonConvert.SerializeObject(data);
            HttpContent httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PUT"), apiUrl)
            {
                Content = httpContent
            };

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string resContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"response: {resContent}");

                var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

                // Retrieve settings
                string connectionString = config["AzureWebJobsStorage"];

              
                var containerName = "output-file-container";

                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

                var blobClient = blobContainerClient.GetBlobClient("output.json");

                byte[] byteArray = Encoding.UTF8.GetBytes(resContent);
                MemoryStream streamContent = new MemoryStream(byteArray);
                await using (var stream = streamContent)
                {
                    await blobClient.UploadAsync(stream, true);
                }

                Console.WriteLine("File uploaded to Azure Blob Storage successfully.");
            }
            else
            {
                Console.WriteLine($"{response.StatusCode}");
            }

        }
    }

    public class InputData
    {
        [JsonPropertyName("subscriptionId")]
        public string? subscriptionId { get; set; } = null;

        [JsonPropertyName("resourceGroup")]
        public string? resourceGroup { get; set; } = null;

        [JsonPropertyName("azureLoadTestingResourceName")]
        public string? azureLoadTestingResourceName { get; set; } = null;

        [JsonPropertyName("loadTestRuns")]
        public IEnumerable<object> loadTestRuns { get; set; } = null;
    }
}
