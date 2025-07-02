using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Azure;
using SkiaSharp;

// Import namespaces
using Azure.AI.Vision.ImageAnalysis;


namespace image_analysis
{
    class Program
    {
        
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
                ImageAnalysisClient client = new ImageAnalysisClient(
                    new Uri(aiSvcEndpoint),
                    new AzureKeyCredential(aiSvcKey));


                // Analyze image
                using FileStream stream = new FileStream(imageFile, FileMode.Open);
                Console.WriteLine($"\nAnalyzing {imageFile} \n");

                ImageAnalysisResult result = client.Analyze(
                    BinaryData.FromStream(stream),
                    VisualFeatures.Caption | 
                    VisualFeatures.DenseCaptions |
                    VisualFeatures.Objects |
                    VisualFeatures.Tags |
                    VisualFeatures.People);

      
                // Get image captions
                if (result.Caption.Text != null)
                {
                    Console.WriteLine("\nCaption:");
                    Console.WriteLine($"   \"{result.Caption.Text}\", Confidence {result.Caption.Confidence:0.00}\n");
                }

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
                        Console.WriteLine($"   '{tag.Name}', Confidence: {tag.Confidence:P2}");
                    }
                }
                

                // Get objects in the image
                if (result.Objects.Values.Count > 0)
                {
                    Console.WriteLine("\nObjects:");
                    foreach (DetectedObject detectedObject in result.Objects.Values)
                    {
                        // PPrint object tag and confidence
                        Console.WriteLine($"  {detectedObject.Tags[0].Name} ({detectedObject.Tags[0].Confidence:P2})");
                    }
                    // Annotate objects in the image
                    await ShowObjects(imageFile, result.Objects);

                }
                

                // Get people in the image
                if (result.People.Values.Count > 0)
                {
                    Console.WriteLine($" People:");

                    foreach (DetectedPerson person in result.People.Values)
                    {
                        // Print location and confidence of each person detected
                        if (person.Confidence > 0.2)
                        {
                            Console.WriteLine($"   Bounding box {person.BoundingBox}, Confidence: {person.Confidence:P2}");
                        }
                    }
                    // Annotate people in the image
                    await ShowPeople(imageFile, result.People);
                }
                

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task ShowObjects(string imageFile, ObjectsResult detectedObjects)
        {
            Console.WriteLine("\nAnnotating objects...");

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
            
            foreach (DetectedObject detectedObject in detectedObjects.Values)
            {
                // Draw object bounding box
                var r = detectedObject.BoundingBox;
                SKRect rect = new SKRect(r.X, r.Y, r.X + r.Width, r.Y + r.Height);
                canvas.DrawRect(rect, paint);
            }

            // Save the annotated image
            var objectFile = "objects.jpg";
            using SKFileWStream output = new SKFileWStream(objectFile);
            bitmap.Encode(output, SKEncodedImageFormat.Jpeg, 100);
            Console.WriteLine($"  Results saved in {objectFile}\n");
        }

        static async Task ShowPeople(string imageFile, PeopleResult detectedPeople)
        {
            Console.WriteLine("\nAnnotating people...");

            using SKBitmap bitmap = SKBitmap.Decode(imageFile);
            using SKCanvas canvas = new SKCanvas(bitmap);

            SKPaint paint = new SKPaint
            {
                Color = SKColors.Cyan,
                StrokeWidth = 3,
                Style = SKPaintStyle.Stroke
            };

            foreach (DetectedPerson person in detectedPeople.Values)
            {
                if (person.Confidence > 0.2)
                {
                    // Draw bounding box
                    var r = person.BoundingBox;
                    SKRect rect = new SKRect(r.X, r.Y, r.X + r.Width, r.Y + r.Height);
                    canvas.DrawRect(rect, paint);
                }
            }

            // Save the annotated image
            var peopleFile = "people.jpg";
            using SKFileWStream output = new SKFileWStream(peopleFile);
            bitmap.Encode(output, SKEncodedImageFormat.Jpeg, 100);
            Console.WriteLine($"  Results saved in {peopleFile}\n");
        }
    }
}
