using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace ChromeDriversWithNoChromes
{
    internal class CommandLine
    {
        public Dictionary<string, object> Slashed { get; set; }
        public List<string> Args { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var CommandLineResults = ParseCommandLineToSettingsAndArgs(args);
            var processList = FindProcesses(new string[] { "chromedriver" });
            if (processList.Count > 0)
            {
                Console.WriteLine($"{processList.Count} chromedrivers found.");

                foreach (var proc in processList)
                {
                    var descendants = GetChildren(proc);
                    string tag = string.Empty;

                    if (descendants.Count() == 0)
                    {
                        if (CommandLineResults.Slashed.ContainsKey("/K"))
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

        static CommandLine ParseCommandLineToSettingsAndArgs(string[] args)
        {
            var results = new CommandLine
            {
                Slashed = new Dictionary<string, object>(),
                Args = new List<string>()
            };

            foreach (string arg in args)
            {
                if (arg.StartsWith("/"))
                {
                    var colonPos = arg.IndexOf(":");
                    if (colonPos > -1)
                    {
                        results.Slashed[arg.Substring(0, colonPos)] = arg.Substring(colonPos + 1);
                    }
                    else
                    {
                        results.Slashed[arg] = true;
                    }
                }
                else
                {
                    results.Args.Add(arg);
                }
            }
            return results;
        }

        static List<Process> FindProcesses(string[] processNames)
        {
            var results = new List<Process>();
            foreach (string name in processNames)
            {
                var processes = Process.GetProcessesByName(name);
                if (processes.Count() > 0)
                {
                    results.AddRange(processes);
                }
            }
            return results;
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
