using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Ravi.DurableFunctions.HelloWorld.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ravi.DurableFunctions.HelloWorld
{
    public static class ProcessVideoActivities
    {
        [FunctionName("TranscodeVideo")]
        public static async Task<FormatVideoRequest> TranscodeVideo(
            [ActivityTrigger] FormatVideoRequest formatVideoRequest, ILogger logger)
        {
            logger.LogInformation($"Running TranscodeVideo activity for {formatVideoRequest.FileName} file");
            await Task.Delay(1000);
            return new FormatVideoRequest { FileName = "transcoded.mp4" };
        }

        [FunctionName("ExtractThumbnail")]
        public static async Task<FormatVideoRequest> ExtractThumbnail(
            [ActivityTrigger] FormatVideoRequest formatVideoRequest, ILogger logger)
        {
            logger.LogInformation($"Running ExtractThumbnail activity for {formatVideoRequest.FileName} file");
            await Task.Delay(1000);
            return new FormatVideoRequest { FileName = "thumbnail.png" };
        }

        [FunctionName("PrependIntro")]
        public static async Task<FormatVideoRequest> PrependIntroVideo(
            [ActivityTrigger] FormatVideoRequest formatVideoRequest, ILogger logger)
        {
            logger.LogInformation($"Running PrependIntroVideo activity for {formatVideoRequest.FileName} file");
            await Task.Delay(1000);
            return new FormatVideoRequest { FileName = "prependintro.mp4" };
        }
    }
}
