using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ravi.DurableFunctions.HelloWorld.Dtos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ravi.DurableFunctions.HelloWorld
{
    public static class ProcessVideoActivities
    {
    
        [FunctionName("TranscodeVideo")]
        public static async Task<FormatVideoRequest> TranscodeVideo(
            [ActivityTrigger] FormatVideoRequest formatVideoRequest, ILogger logger)
        {
            logger.LogInformation($"Running TranscodeVideo activity for {formatVideoRequest.Location} file for bit rate {formatVideoRequest.BitRate}");
            await Task.Delay(1000);
            return new FormatVideoRequest { Location = $"transcoded_{formatVideoRequest.BitRate}.mp4", BitRate = formatVideoRequest.BitRate };
        }

        [FunctionName("ExtractThumbnail")]
        public static async Task<FormatVideoRequest> ExtractThumbnail(
            [ActivityTrigger] FormatVideoRequest formatVideoRequest, ILogger logger)
        {
            logger.LogInformation($"Running ExtractThumbnail activity for {formatVideoRequest.Location} file");
            await Task.Delay(1000);
            return new FormatVideoRequest { Location = "thumbnail.png" };
        }

        [FunctionName("PrependIntro")]
        public static async Task<FormatVideoRequest> PrependIntroVideo(
            [ActivityTrigger] FormatVideoRequest formatVideoRequest, ILogger logger)
        {
            logger.LogInformation($"Running PrependIntroVideo activity for {formatVideoRequest.Location} file");
            await Task.Delay(1000);
            return new FormatVideoRequest { Location = "prependintro.mp4" };
        }

        [FunctionName("RetrieveBitRates")]
        public static async Task<IEnumerable<int>> RetrieveBitRates(
            [ActivityTrigger] object input, ILogger logger, ExecutionContext context)
        {
            logger.LogInformation("Retrieving bit rates from configuration");

            var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

            var bitRatesFromConfig = config["TranscodeBitrates"]; 
            var bitRates = bitRatesFromConfig.Split(",").Select(i => int.Parse(i));
            return await Task.FromResult(bitRates);
        }

        [FunctionName("CleanUp")]
        public static async Task Cleanup(
            [ActivityTrigger] FormatVideoRequest formatVideoRequest, ILogger logger)
        {
            logger.LogInformation($"Cleaning up resources for file {formatVideoRequest.Location}");
            await Task.Delay(1000);
        }

       
        [FunctionName("SendApprovalRequestEmail")]
        public static async Task SendApprovalEmail(
            [ActivityTrigger] FormatVideoRequest formatVideoRequest, ILogger logger)
        {
            logger.LogInformation($"Sending email for approval. File: {formatVideoRequest.Location}");
            await Task.Delay(1000);
        }

        [FunctionName("PublishVideo")]
        public static async Task PublishVideoToCloud(
            [ActivityTrigger] FormatVideoRequest formatVideoRequest, ILogger logger)
        {
            logger.LogInformation($"Publishing video file [{formatVideoRequest.Location}] to cloud");
            await Task.Delay(1000);
        }

        [FunctionName("RejectVideo")]
        public static async Task DeleteVideoFile(
            [ActivityTrigger] FormatVideoRequest formatVideoRequest, ILogger logger)
        {
            logger.LogInformation($"Rejecting and deleting file {formatVideoRequest.Location}");
            await Task.Delay(1000);
        }
    }
}
