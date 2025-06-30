using System;
using Azure;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

// Add references
using Azure.Identity;
using Azure.AI.Projects;
using Azure.AI.Inference;


namespace chat_app
{
    class Program
    {
        static void Main(string[] args)
        {
            // Clear the console
            Console.Clear();
            
            try
            {
                // Get configuration settings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string project_connection = configuration["PROJECT_CONNECTION"];
                string model_deployment = configuration["MODEL_DEPLOYMENT"];



                // Initialize the project client



                // Get a chat client
                DefaultAzureCredentialOptions options = new() { 
                    ExcludeEnvironmentCredential = true,
                    ExcludeManagedIdentityCredential = true
                };
                var projectClient = new AIProjectClient(
                    new Uri(project_connection),
                    new DefaultAzureCredential(options));
                ChatCompletionsClient chat = projectClient.GetChatCompletionsClient();




                // Initialize prompts
                string system_message = "You are an AI assistant in a grocery store that sells fruit.";
                string prompt = "";

                // Loop until the user types 'quit'
                while (prompt.ToLower() != "quit")
                {
                    // Get user input
                    Console.WriteLine("\nAsk a question about the image\n(or type 'quit' to exit)\n");
                    prompt = Console.ReadLine().ToLower();
                    if (prompt == "quit")
                    {
                        break;
                    }
                    else if (prompt.Length < 1)
                    {
                        Console.WriteLine("Please enter a question.\n");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("Getting a response ...\n");


                        // Get a response to image input
                        string imagePath = "mystery-fruit.jpeg";
                        string mimeType = "image/jpeg";
                            
                        // Read and encode the image file
                        byte[] imageBytes = File.ReadAllBytes(imagePath);
                        var binaryImage = new BinaryData(imageBytes);
                            
                        // Include the image file data in the prompt
                        ChatCompletionsOptions requestOptions = new ChatCompletionsOptions()
                        {
                            Messages = {
                                new ChatRequestSystemMessage(system_message),
                                new ChatRequestUserMessage([
                                    new ChatMessageTextContentItem(prompt),
                                    new ChatMessageImageContentItem(bytes: binaryImage, mimeType: mimeType) 
                                ]),
                            },
                            Model = model_deployment
                        };
                        var response = chat.Complete(requestOptions);
                        Console.WriteLine(response.Value.Content);
                        

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

