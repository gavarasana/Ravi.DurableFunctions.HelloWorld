#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Ravi.DurableFunctions.HelloWorld.Dtos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ravi.DurableFunctions.HelloWorld
{
    public static class VideoProcessorOrchestrator
    {
        [FunctionName("VideoProcessor")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {

            var formatVideoRequest = context.GetInput<FormatVideoRequest>();
            var outputs = new List<string>();


            // Replace "hello" with the name of your Durable Activity Function.

            if (!context.IsReplaying)
            {
                logger.LogInformation("Invoking TranscodeVideo activity");
            }
            var transcodedLocation = await context.CallActivityAsync<FormatVideoRequest>("TranscodeVideo", formatVideoRequest);

            if (!context.IsReplaying)
            {
                logger.LogInformation("Invoking thumbnail extract activity");
            }
            var thumbnailLocation = await context.CallActivityAsync<FormatVideoRequest>("ExtractThumbnail", transcodedLocation);

            if (!context.IsReplaying)
            {
                logger.LogInformation("Invoking prepend intro video activity");
            }
            var withIntroLocation = await context.CallActivityAsync<FormatVideoRequest>("PrependIntro", transcodedLocation);

            outputs.Add(transcodedLocation.FileName);
            outputs.Add(thumbnailLocation.FileName);
            outputs.Add(withIntroLocation.FileName);

                return outputs;
        }

        [FunctionName("Function1_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("VideoProcessor_Start")]
        public static async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // var videoFile = req.Query["FileName"][0];
            FormatVideoRequest formatVideoRequest ;
            var buffer = new byte[1024];

            var dataLength = await req.Body.ReadAsync(buffer);
            var data1 = new byte[dataLength];
            Array.Copy(buffer, 0, data1, 0, dataLength);
            using (var memoryStream = new MemoryStream(data1))
            {
                var reader = new StreamReader(memoryStream);
                var bodyAsString = reader.ReadToEnd();
                formatVideoRequest = JsonSerializer.Deserialize<FormatVideoRequest>(bodyAsString, new JsonSerializerOptions {PropertyNameCaseInsensitive = true });
            }

            if ((formatVideoRequest == null) || (string.IsNullOrEmpty(formatVideoRequest.FileName)))
            {
                return new BadRequestResult();
            }

            //var formatVideoRequest = req.Body.Read

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("VideoProcessor", formatVideoRequest);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }



    }
}