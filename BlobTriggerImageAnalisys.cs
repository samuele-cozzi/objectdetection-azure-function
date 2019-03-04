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

namespace Company.Function
{
    public static class BlobTriggerImageAnalisys
    {
        [FunctionName("BlobTriggerImageAnalisys")]
        public static void Run([BlobTrigger("shelfs-milano/{name}", Connection = "generalstoragelux_STORAGE")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            SendToCustomVision(myBlob);
            SendToPowerBI();
        }

        private static void SendToCustomVision(Stream image)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Prediction-Key", "2cb9736dedf34d968aeef6a5b7c01bc1");
                var url = $"https://southcentralus.api.cognitive.microsoft.com/customvision/v2.0/Prediction/9cb32e4c-4553-4327-8342-78962f3ae53c/image";
                HttpContent content = new StreamContent(image);

                var res = client.PostAsync(url, content);

                try
                {
                    var response = res.Result.Content.ReadAsStringAsync().Result;
                    var cognitivePrediction = JsonConvert.DeserializeObject<CognitivePrediction>(response);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        private static void SendToPowerBI()
        {
            using (var client = new HttpClient())
            {
                var url = $"https://api.powerbi.com/beta/8b87af7d-8647-4dc7-8df4-5f69a2011bb5/datasets/87838796-153f-4526-8789-890d6e6bba10/rows?key=Et9UfcttnRjpcL9AtmauYjjRsw2GbWC2oHXln%2FMuZPv4jEn83Vn%2B7Iy34EH81gueRKmPR18fbTN2aeoezBeZww%3D%3D";


                var obj = new PowerBI()
                {
                    Longitude = "9.18951",
                    Latitude = "45.46427",
                    shelf_status = "OK",
                    shelf_count = 100,
                    Longitude_decimal = 9.18951,
                    Latidude_decimal = 45.46427,
                    Shop_name = "Milano"
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
