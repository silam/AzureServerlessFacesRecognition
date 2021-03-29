#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;


public static async Task Run(Stream myBlob, string name, 
IAsyncCollector<FaceRectangle> outputMaleTable,
IAsyncCollector<FaceRectangle> outputFemaleTable,
ILogger log)
{
    log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

    string result = await CallVisionAPI(myBlob, log);
    log.LogInformation(result);

    if ( String.IsNullOrEmpty(result))
    {
        log.LogInformation($"Result is NULL");
        return;
    }

    ImageData ImageData = JsonConvert.DeserializeObject<ImageData>(result);

    foreach (Face face in ImageData.Faces)
    {
        log.LogInformation($"For each Face");
        var faceRectangle = face.FaceRectangle;
        faceRectangle.RowKey = Guid.NewGuid().ToString();
        faceRectangle.PartitionKey = "Functions";
        faceRectangle.ImageFile = name + ".jpg";
        if ( face.Gender == "Female")
        {
            await outputFemaleTable.AddAsync(faceRectangle);
            log.LogInformation($"A Female");
        }
        else
        {
            await outputMaleTable.AddAsync(faceRectangle);
            log.LogInformation($"A Male");


        }
    }

}

static async Task<string> CallVisionAPI(Stream image, ILogger log)
{
    using (var client = new HttpClient())
    {





        var content = new StreamContent(image);
        log.LogInformation($"Stream content");


        var url = "https://westus.api.cognitive.microsoft.com/vision/v1.0/analyze?visualFeatures=Faces&language=en";
        log.LogInformation("Key = " + Environment.GetEnvironmentVariable("Vision_API_Subscription_Key"));
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("Vision_API_Subscription_Key"));
       

        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream"); 

        var httpResponse = await client.PostAsync(url, content);

        log.LogInformation(" response code = " + httpResponse.StatusCode);
        if ( httpResponse.StatusCode == HttpStatusCode.OK)
        {
            log.LogInformation($"Status code is OK");

            return await httpResponse.Content.ReadAsStringAsync();

        }

    }
    return null;
}
public class ImageData {
    public List<Face> Faces {get;set;}
}
public class Face {
    public int Age {get;set;}
    public string Gender {get;set;}
    public FaceRectangle FaceRectangle {get;set;}
}
public class FaceRectangle: TableEntity{
    public string ImageFile {get;set;}
    public int Left {get;set;}
    public int Top {get;set;}
    public int Width  {get;set;}
    public int Height {get;set;} 

    
}
