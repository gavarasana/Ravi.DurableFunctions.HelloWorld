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
using System.Linq;
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
            var outputs = new List<string>();
            var formatVideoRequest = context.GetInput<FormatVideoRequest>();
            var approvalResult = "Unknown";
            try
            {


                var allTasksResult = await context.CallSubOrchestratorAsync<IEnumerable<FormatVideoRequest>>("TranscodeVideoOrchestor", formatVideoRequest);
                var extractThumbnailRequest = allTasksResult.OrderByDescending(r => r.BitRate).First();

                if (!context.IsReplaying)
                {
                    logger.LogInformation("Invoking thumbnail extract activity");
                }
                var thumbnailLocation = await context.CallActivityAsync<FormatVideoRequest>("ExtractThumbnail", extractThumbnailRequest);

                if (!context.IsReplaying)
                {
                    logger.LogInformation("Invoking prepend intro video activity");
                }
                var withIntroLocation = await context.CallActivityAsync<FormatVideoRequest>("PrependIntro", extractThumbnailRequest);


                await context.CallActivityAsync("SendApprovalRequestEmail", withIntroLocation);

                approvalResult = await context.WaitForExternalEvent<string>("ApprovalResult");

                if (approvalResult == "Approved")
                {
                    await context.CallActivityAsync("PublishVideo", withIntroLocation);
                }
                else
                {
                    await context.CallActivityAsync("RejectVideo", withIntroLocation);
                }

                outputs.Add(extractThumbnailRequest.Location);
                outputs.Add(thumbnailLocation.Location);
                outputs.Add(withIntroLocation.Location);
                outputs.Add(approvalResult);

            }
            catch (Exception)
            {
                await context.CallActivityAsync("CleanUp", formatVideoRequest);
                outputs.Add("Failed");
            }

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
            FormatVideoRequest formatVideoRequest;
            var buffer = new byte[1024];

            var dataLength = await req.Body.ReadAsync(buffer);
            var data1 = new byte[dataLength];
            Array.Copy(buffer, 0, data1, 0, dataLength);
            using (var memoryStream = new MemoryStream(data1))
            {
                var reader = new StreamReader(memoryStream);
                var bodyAsString = reader.ReadToEnd();
                formatVideoRequest = JsonSerializer.Deserialize<FormatVideoRequest>(bodyAsString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            if ((formatVideoRequest == null) || (string.IsNullOrEmpty(formatVideoRequest.Location)))
            {
                return new BadRequestResult();
            }

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("VideoProcessor", formatVideoRequest);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("TranscodeVideoOrchestor")]
        public static async Task<IEnumerable<FormatVideoRequest>> TranscodeVideoOrchestor(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            if (!context.IsReplaying)
            {
                logger.LogInformation("Invoking TranscodeVideo activity");
            }

            var formatVideoRequest = context.GetInput<FormatVideoRequest>();

            var bitRatesToProcess = await context.CallActivityAsync<IEnumerable<int>>("RetrieveBitRates", null);
            var transCodeVideoTasks = new List<Task<FormatVideoRequest>>();

            foreach (var item in bitRatesToProcess)
            {
                if (!context.IsReplaying)
                {
                    logger.LogInformation($"Invoking TranscodeVideo activity for bit rate {item}");
                }
                transCodeVideoTasks.Add(context.CallActivityAsync<FormatVideoRequest>("TranscodeVideo",
                    new FormatVideoRequest { Location = formatVideoRequest.Location, BitRate = item }));
            }

            return await Task.WhenAll(transCodeVideoTasks);
        }


    }
}