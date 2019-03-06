using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Company.Function.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Company.Function
{
    public static class BlobTriggerImageAnalisys
    {
        [FunctionName("BlobTriggerImageAnalisys")]
        public static void Run([BlobTrigger("shelf/{name}", Connection = "storageluxottica_STORAGE")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            CognitivePrediction prediction = SendToCustomVision(myBlob);
            SendToPowerBI(prediction);
        }

        private static CognitivePrediction SendToCustomVision(Stream image)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Prediction-Key", "ed42fdeaa19a4f7d8409fee6d30d345b");
                var url = $"https://southcentralus.api.cognitive.microsoft.com/customvision/v2.0/Prediction/190d83a1-0fcb-4137-b79e-4ce5d015ce59/image";
                
                HttpContent content = new StreamContent(image);

                var res = client.PostAsync(url, content);

                try
                {
                    var response = res.Result.Content.ReadAsStringAsync().Result;
                    var cognitivePrediction = JsonConvert.DeserializeObject<CognitivePrediction>(response);
                    cognitivePrediction.predictions = cognitivePrediction.predictions.Where(x => x.probability > 0.40).ToList();
                    return cognitivePrediction;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return null;
                }
            }
        }

        private static void SendToPowerBI(CognitivePrediction prediction)
        {
            using (var client = new HttpClient())
            {
                var url = $"https://api.powerbi.com/beta/8b87af7d-8647-4dc7-8df4-5f69a2011bb5/datasets/87838796-153f-4526-8789-890d6e6bba10/rows?key=Et9UfcttnRjpcL9AtmauYjjRsw2GbWC2oHXln%2FMuZPv4jEn83Vn%2B7Iy34EH81gueRKmPR18fbTN2aeoezBeZww%3D%3D";

                decimal order = 0M; 

                var web = prediction.predictions.FirstOrDefault(x => x.tagName == "web");
                var rayban = prediction.predictions.FirstOrDefault(x => x.tagName == "rayban");
                var oklay = prediction.predictions.FirstOrDefault(x => x.tagName == "oklay");

                if (web == null || rayban == null || oklay == null){
                    order = 1M;
                }
                else {
                    if (rayban.boundingBox.left < web.boundingBox.left ){
                        order = order + 1;
                    }
                    if (rayban.boundingBox.left < oklay.boundingBox.left ){
                        order = order + 1;
                    }
                    if (oklay.boundingBox.left < web.boundingBox.left ){
                        order = order + 1;
                    }
                    if (oklay.boundingBox.top < web.boundingBox.top ){
                        order = order + 1;
                    }
                    if (oklay.boundingBox.top < rayban.boundingBox.top ){
                        order = order + 1;
                    }
                }

                var obj = new PowerBI()
                {
                    shelf_status = "OK",
                    shelf_count = prediction.predictions.Select(x => x.tagName).Distinct().Count(),
                    web_count = (prediction.predictions.Count(x => x.tagName == "web") > 0) ? 1 : 0,
                    raiban_count = (prediction.predictions.Count(x => x.tagName == "rayban") > 0) ? 1 : 0,
                    oklay_count = (prediction.predictions.Count(x => x.tagName == "oklay") > 0) ? 1 : 0,
                    min = 0,
                    max = 1,
                    order_threshold = 0.8M,
                    order_value = order / 5,
                    Data = DateTime.UtcNow.ToString("o")
                };

                var request = new PowerBI[] { obj };

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var res = client.PostAsync(url, content);

                try
                {
                    res.Result.EnsureSuccessStatusCode();    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
