using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;


public class HomeController : Controller
{
    private const string VisionApiKey = "";
    private const string VisionEndpoint = "";

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UploadImages(List<IFormFile> files)
    {
        var altTexts = new List<string>();

        foreach (var file in files.Where(f => f.Length > 0))
        {
            using (var stream = file.OpenReadStream())
            {
                string altText = await GetAltTextAsync(stream);
                altTexts.Add(altText);
            }
        }

        // Retrieve existing alt text data from TempData
        if (TempData.TryGetValue("AltTexts", out var existingAltTexts) && existingAltTexts is List<string> existingAltTextList)
        {
            existingAltTextList.AddRange(altTexts);
            TempData["AltTexts"] = existingAltTextList;
        }
        else
        {
            TempData["AltTexts"] = altTexts.ToList();
        }

        return Json(new { success = true, altTexts });
    }


    public IActionResult DownloadAltTextFile()
    {
        // Retrieve existing alt text data from TempData
        var tempDataValue = TempData["AltTexts"];
        List<string> altTexts = null;

        if (tempDataValue is string[] stringArray)
        {
            // Convert string array to List<string>
            altTexts = stringArray.ToList();
        }
        else if (tempDataValue is List<string> stringList)
        {
            altTexts = stringList;
        }
        else
        {
            // Unexpected type, handle accordingly (or throw an exception)
            return BadRequest("Unexpected type in TempData['AltTexts']");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(string.Join("\n", altTexts));

        // Use the helper method to return the file as a downloadable text file
        return DownloadTextFile(bytes, "AltTexts.txt");
    }

    private IActionResult DownloadTextFile(byte[] content, string fileName)
    {
        return File(content, "text/plain", fileName);
    }

    private async Task<string> GetAltTextAsync(Stream stream)
    {
        var client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(VisionApiKey))
        {
            Endpoint = VisionEndpoint
        };

        var imageFeatures = new List<VisualFeatureTypes?> { VisualFeatureTypes.Description };

        var analysis = await client.AnalyzeImageInStreamAsync(stream, imageFeatures);

        if (analysis.Description.Captions.Count > 0)
        {
            return analysis.Description.Captions[0].Text;
        }

        return "No description available.";
    }
}
