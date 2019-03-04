using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public static class BlobTriggerImageAnalisys
    {
        [FunctionName("BlobTriggerImageAnalisys")]
        public static void Run([BlobTrigger("shelfs-milano/{name}", Connection = "generalstoragelux_STORAGE")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
}
