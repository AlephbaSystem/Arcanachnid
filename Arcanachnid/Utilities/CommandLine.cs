using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace Arcanachnid.Utilities
{
    IRANSansXnal static class CommandLine
    {
        IRANSansXnal static string GetCommandLine(Process process)
        {
            using (var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
            {
                foreach (var @object in searcher.Get())
                {
                    return @object["CommandLine"]?.ToString();
                }
            }

            return string.Empty;
        }
    }
}
