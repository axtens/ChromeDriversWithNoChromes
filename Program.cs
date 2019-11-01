using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace ChromeDriversWithNoChromes
{
    class Program
    {
        static readonly Dictionary<string, object> Settings = new Dictionary<string, object>();
        static readonly List<string> Args = new List<string>();

        static readonly List<Process> FoundProcesses = new List<Process>();
        static void Main(string[] args)
        {
            ParseCommandLineToSettingsAndArgs(args);
            var count = StoreProcesses(new string[] { "chromedriver" });
            if (count > 0)
            {
                Console.WriteLine($"{count} chromedrivers found.");

                foreach (var proc in FoundProcesses)
                {
                    var descendants = GetChildren(proc);
                    string tag = string.Empty;

                    if (descendants.Count() == 0)
                    {
                        if (Settings.ContainsKey("/K"))
                        {
                            try
                            {
                                proc.Kill();
                                tag = "TERMINATED.";
                            }
                            catch
                            {
                                tag = "COULD NOT TERMINATE";
                            }
                        }
                        else
                        {
                            tag = "COULD BE TERMINATED";
                        }
                    }
                    else
                    {
                        tag = "IGNORED";
                    }

                    Console.WriteLine($"{tag} {proc.ProcessName}[{proc.Id}]({GetCommandLine(proc)}), {descendants.Count()} descendant/s");

                }
            }
        }

        static void ParseCommandLineToSettingsAndArgs(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith("/"))
                {
                    var colonPos = arg.IndexOf(":");
                    if (colonPos > -1)
                    {
                        Settings[arg.Substring(0, colonPos)] = arg.Substring(colonPos + 1);
                    }
                    else
                    {
                        Settings[arg] = true;
                    }
                }
                else
                {
                    Args.Add(arg);
                }
            }
        }

        static int StoreProcesses(string[] processNames)
        {
            int count = 0;
            foreach (string name in processNames)
            {
                var processes = Process.GetProcessesByName(name);
                if (processes.Count() > 0)
                {
                    FoundProcesses.AddRange(processes);
                    count += processes.Count();
                }
            }
            return count;
        }

        static Process[] GetChildren(Process process)
        {
            try
            {
                using (var query = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Process WHERE ParentProcessId={process.Id}"))
                {
                    return query
                        .Get()
                        .OfType<ManagementObject>()
                        .Select(p => Process.GetProcessById((int)(uint)p["ProcessId"]))
                        .ToArray();
                }
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine(ae.Message);
                return null;
            }
        }

        static string GetCommandLine(Process proc)
        {
            string commandLine = string.Empty;
            try
            {
                ManagementObjectSearcher commandLineSearcher = new ManagementObjectSearcher(
                    "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + proc.Id);

                foreach (ManagementObject commandLineObject in commandLineSearcher.Get())
                {
                    commandLine += (String)commandLineObject["CommandLine"];
                }
            }
            catch
            {
                commandLine = $"Command-line data not available";
            }

            return commandLine;
        }
    }
}
