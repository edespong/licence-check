using System.Diagnostics;
using System.Threading.Tasks;

namespace LicenseInspector
{
    /// <summary>
    /// Helper class for running external processes.
    /// </summary>
    public static class ProcessHelper
    {
        /// <summary>
        /// Starts a process at the given path with the given arguments.
        /// </summary>
        /// <returns>
        /// Task returning the exit code and full output once the process has
        /// exited.
        /// </returns>
        public static Task<(int, string)> RunProcessAsync(string path, string args)
        {
            var tcs = new TaskCompletionSource<(int, string)>();
            var process = new Process
            {
                StartInfo = {
                    FileName = path,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, _) =>
            {
                string stdout = process.StandardOutput.ReadToEnd();
                tcs.SetResult((process.ExitCode, stdout));
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }
    }
}
