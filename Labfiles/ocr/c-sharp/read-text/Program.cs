using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure;
using SkiaSharp;

// Import namespaces
using Azure.AI.Vision.ImageAnalysis;

namespace read_text
{
    class Program
    {

        // Declare variable for Azure AI Vision client
        private static ImageAnalysisClient client;

        static void Main(string[] args)
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
                string imageFile = "images/Lincoln.jpg";
                if (args.Length > 0)
                {
                    imageFile = args[0];
                }
                
                // Authenticate Azure AI Vision client
                client = new ImageAnalysisClient(
                    new Uri(aiSvcEndpoint),
                    new AzureKeyCredential(aiSvcKey));

                
                // Read text in image
                GetTextRead(imageFile);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void GetTextRead(string imageFile)
        {
            Console.WriteLine($"\nReading text from {imageFile} \n");

            // Use a file stream to pass the image data to the analyze call
            using FileStream stream = new FileStream(imageFile,
                                                     FileMode.Open);

            // Use Analyze image function to read text in image
            ImageAnalysisResult result = client.Analyze(
                BinaryData.FromStream(stream),
                // Specify the features to be retrieved
                VisualFeatures.Read);
                
            stream.Close();
                
            // Display analysis results
            if (result.Read != null)
            {
                Console.WriteLine($"Text:");
                
                // Load the image using SkiaSharp
                using SKBitmap bitmap = SKBitmap.Decode(imageFile);
                // Create canvas to draw on the bitmap
                using SKCanvas canvas = new SKCanvas(bitmap);

                // Create paint for drawing polygons (bounding boxes)
                SKPaint paint = new SKPaint
                {
                    Color = SKColors.Cyan,
                    StrokeWidth = 3,
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true
                };

                foreach (var line in result.Read.Blocks.SelectMany(block => block.Lines))
                {

                    // Return the text detected in the image
                    Console.WriteLine($"   '{line.Text}'");
                        
                    // Draw bounding box around line
                    bool drawLinePolygon = true;
                        
                    // Return the position bounding box around each line
                    Console.WriteLine($"   Bounding Polygon: [{string.Join(" ", line.BoundingPolygon)}]");
                        
                        
                        
                    // Find individual words in the line
                    foreach (DetectedTextWord word in line.Words)
                    {
                        Console.WriteLine($"     Word: '{word.Text}', Confidence {word.Confidence:F4}, Bounding Polygon: [{string.Join(" ", word.BoundingPolygon)}]");
                            
                        // Draw word bounding polygon
                        drawLinePolygon = false;
                        var r = word.BoundingPolygon;
                        
                        // Convert the bounding polygon into an array of SKPoints    
                        SKPoint[] polygonPoints = new SKPoint[]
                        {
                            new SKPoint(r[0].X, r[0].Y),
                            new SKPoint(r[1].X, r[1].Y),
                            new SKPoint(r[2].X, r[2].Y),
                            new SKPoint(r[3].X, r[3].Y)
                        };

                        // Draw the word polygon on the canvas
                        DrawPolygon(canvas, polygonPoints, paint);
                    }
                        
                        
                        
                    // Draw line bounding polygon
                    if (drawLinePolygon)
                    {
                        var r = line.BoundingPolygon;
                        SKPoint[] polygonPoints = new SKPoint[]
                        {
                            new SKPoint(r[0].X, r[0].Y),
                            new SKPoint(r[1].X, r[1].Y),
                            new SKPoint(r[2].X, r[2].Y),
                            new SKPoint(r[3].X, r[3].Y)
                        };

                        // Call helper method to draw a polygon
                        DrawPolygon(canvas, polygonPoints, paint);
                    }
                
                
                }
                        
                // Save the annotated image using SkiaSharp
                var textFile = "text.jpg";
                using (SKFileWStream output = new SKFileWStream(textFile))
                {
                    // Encode the bitmap into JPEG format with full quality (100)
                    bitmap.Encode(output, SKEncodedImageFormat.Jpeg, 100);
                }

                Console.WriteLine($"\nResults saved in {textFile}\n");
            }
            
    
        }


        // Helper method to draw a polygon given an array of SKPoints
        static void DrawPolygon(SKCanvas canvas, SKPoint[] points, SKPaint paint)
        {
            if (points == null || points.Length == 0)
                return;

            using (var path = new SKPath())
            {
                path.MoveTo(points[0]);
                for (int i = 1; i < points.Length; i++)
                {
                    path.LineTo(points[i]);
                }
                path.Close();
                canvas.DrawPath(path, paint);
            }
        }
    }
}

