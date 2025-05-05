using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure;
using SkiaSharp;
using Azure.AI.Vision.ImageAnalysis;

// Import namespaces

namespace image_analysis
{
    class Program
    {
        // Declare variable for Azure AI Vision client
        private static ImageAnalysisClient client;

        static async Task Main(string[] args)
        {
            // Clear the console
            Console.Clear();
            
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string aiSvcEndpoint = configuration["AIServicesEndpoint"];
                string aiSvcKey = configuration["AIServicesKey"];

                // Get image
                string imageFile = "images/street.jpg";
                if (args.Length > 0)
                {
                    imageFile = args[0];
                }

                // Authenticate Azure AI Vision client
                client = new ImageAnalysisClient(
                    new Uri(aiSvcEndpoint),
                    new AzureKeyCredential(aiSvcKey));
                
                // Analyze image
                await AnalyzeImage(imageFile, client);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task AnalyzeImage(string imageFile, ImageAnalysisClient client)
        {
            Console.WriteLine($"\nAnalyzing {imageFile} \n");

            // Use a file stream to pass the image data to the analyze call
            using FileStream stream = new FileStream(imageFile,
                                                     FileMode.Open);

            // Get result with specified features to be retrieved
            ImageAnalysisResult result = client.Analyze(
                BinaryData.FromStream(stream),
                VisualFeatures.Caption | 
                VisualFeatures.DenseCaptions |
                VisualFeatures.Objects |
                VisualFeatures.Tags |
                VisualFeatures.People);
            
            // Display analysis results
            // Get image captions
            if (result.Caption.Text != null)
            {
                Console.WriteLine(" Caption:");
                Console.WriteLine($"   \"{result.Caption.Text}\", Confidence {result.Caption.Confidence:0.00}\n");
            }
    
            // Get image dense captions
            Console.WriteLine(" Dense Captions:");
            foreach (DenseCaption denseCaption in result.DenseCaptions.Values)
            {
                Console.WriteLine($"   Caption: '{denseCaption.Text}', Confidence: {denseCaption.Confidence:0.00}");
            }
    
            // Get image tags
            if (result.Tags.Values.Count > 0)
            {
                Console.WriteLine($"\n Tags:");
                foreach (DetectedTag tag in result.Tags.Values)
                {
                    Console.WriteLine($"   '{tag.Name}', Confidence: {tag.Confidence:F2}");
                }
            }
                
            // Get objects in the image
            if (result.Objects.Values.Count > 0)
            {
                Console.WriteLine(" Objects:");

                // Load the image using SkiaSharp
                using SKBitmap bitmap = SKBitmap.Decode(imageFile);
                using SKCanvas canvas = new SKCanvas(bitmap);

                // Set up styles for drawing
                SKPaint paint = new SKPaint
                {
                    Color = SKColors.Cyan,
                    StrokeWidth = 3,
                    Style = SKPaintStyle.Stroke
                };

                SKPaint textPaint = new SKPaint
                {
                    Color = SKColors.Cyan,
                    IsAntialias = true
                };

                SKFont textFont = new SKFont(SKTypeface.Default,24,1,0);

                foreach (DetectedObject detectedObject in result.Objects.Values)
                {
                    Console.WriteLine($"   \"{detectedObject.Tags[0].Name}\"");

                    // Draw object bounding box
                    var r = detectedObject.BoundingBox;
                    SKRect rect = new SKRect(r.X, r.Y, r.X + r.Width, r.Y + r.Height);
                    canvas.DrawRect(rect, paint);

                    // Draw label
                    canvas.DrawText(detectedObject.Tags[0].Name, r.X, r.Y - 5, SKTextAlign.Left, textFont, textPaint);
                }

                // Save the annotated image
                var objectFile = "objects.jpg";
                using SKFileWStream output = new SKFileWStream(objectFile);
                bitmap.Encode(output, SKEncodedImageFormat.Jpeg, 100);
                Console.WriteLine($"  Results saved in {objectFile}\n");
            }    
                
            // Get people in the image
            if (result.People.Values.Count > 0)
            {
                Console.WriteLine($" People:");

                using SKBitmap bitmap = SKBitmap.Decode(imageFile);
                using SKCanvas canvas = new SKCanvas(bitmap);

                SKPaint paint = new SKPaint
                {
                    Color = SKColors.Cyan,
                    StrokeWidth = 3,
                    Style = SKPaintStyle.Stroke
                };

                foreach (DetectedPerson person in result.People.Values)
                {
                    // Draw bounding box
                    var r = person.BoundingBox;
                    SKRect rect = new SKRect(r.X, r.Y, r.X + r.Width, r.Y + r.Height);
                    canvas.DrawRect(rect, paint);

                    // Print location and confidence of each person detected
                    Console.WriteLine($"   Bounding box {person.BoundingBox}, Confidence: {person.Confidence:F2}");
                }

                // Save the annotated image
                var peopleFile = "people.jpg";
                using SKFileWStream output = new SKFileWStream(peopleFile);
                bitmap.Encode(output, SKEncodedImageFormat.Jpeg, 100);
                Console.WriteLine($"  Results saved in {peopleFile}\n");
            }

        }
    }
}
