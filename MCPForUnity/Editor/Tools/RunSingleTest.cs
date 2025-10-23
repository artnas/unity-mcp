using System;
using System.Threading.Tasks;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Resources.Tests;
using MCPForUnity.Editor.Services;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools
{
    [McpForUnityTool("RunSingleTest")]
    public static class RunSingleTest
    {
        private const int DefaultTimeoutSeconds = 600;

        public static async Task<object> HandleCommand(JObject @params)
        {
            string testName = @params?["testName"]?.ToString();
            if (string.IsNullOrWhiteSpace(testName))
            {
                return Response.Error("testName parameter is required");
            }

            string modeStr = @params?["mode"]?.ToString();
            if (string.IsNullOrWhiteSpace(modeStr))
            {
                modeStr = "play";
            }

            if (!ModeParser.TryParse(modeStr, out var parsedMode, out var parseError))
            {
                return Response.Error(parseError);
            }

            int timeoutSeconds = DefaultTimeoutSeconds;
            try
            {
                var timeoutToken = @params?["timeoutSeconds"];
                if (timeoutToken != null && int.TryParse(timeoutToken.ToString(), out var parsedTimeout) && parsedTimeout > 0)
                {
                    timeoutSeconds = parsedTimeout;
                }
            }
            catch
            {
            }

            var testService = MCPServiceLocator.Tests;
            Task<TestRunResult> runTask;
            try
            {
                runTask = testService.RunSingleTestAsync(parsedMode.Value, testName);
            }
            catch (Exception ex)
            {
                return Response.Error($"Failed to start test run: {ex.Message}");
            }

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var completed = await Task.WhenAny(runTask, timeoutTask).ConfigureAwait(true);

            if (completed != runTask)
            {
                return Response.Error($"Test run timed out after {timeoutSeconds} seconds");
            }

            var result = await runTask.ConfigureAwait(true);

            string message =
                $"{parsedMode.Value} test '{testName}' completed: {result.Passed}/{result.Total} passed, {result.Failed} failed, {result.Skipped} skipped";

            var data = result.ToSerializable(parsedMode.Value.ToString());
            return Response.Success(message, data);
        }
    }
}

