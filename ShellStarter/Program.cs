﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using CommandLine;
using StarterModel;

namespace Starter
{

    /// <summary>
    /// Main
    /// </summary>
    internal class Program
    {
        public static void Main(string[] args)
        {
            UnityCodeSmellAnalyzer ucsa = new UnityCodeSmellAnalyzer();
            Parser.Default.ParseArguments<Options>(args).WithParsed(o => ucsa.Init(o));
        }
    }


    /// <summary>
    /// Starter. Handles one thread for code analysis and one thread for data analysis, one repository at time.
    /// </summary>
    public class UnityCodeSmellAnalyzer
    {

        protected List<string> repos = new List<string>();
        protected ThreadHandler codeAnalysis;
        protected ThreadHandler dataAnalysis;
        protected enum OS { Unix, Windows };
        protected OS os = OS.Windows;

        public UnityCodeSmellAnalyzer()
        {
            Logger.SetLogLevel(1);
            Logger.Start();
        }

        /// <summary>
        /// Init the main program with Opt Provided
        /// </summary>
        /// <param name="o">Opt provided</param>
        public void Init(Options o)
        {
            Logger.Verbose = o.Verbose;
            string[] directories = Directory.GetDirectories(o.Directory);
            foreach (string d in directories) repos.Add(d);
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                os = OS.Unix;
            WriteOutput("Running on " + RuntimeInformation.OSDescription + " " + RuntimeInformation.OSArchitecture);
            WriteOutput(os.ToString());
            WriteOutput("Starting ......");
            WriteOutput("Found " + repos.Count + " Directories");
            foreach (string d in repos)
            {
                StartThread(d);
                while(!codeAnalysis.Finished || !dataAnalysis.Finished) { }
                WriteOutput("---------------------------------------------");
            }
            WriteOutput("ShellStarter Exited");
        }
        /// <summary>
        /// Starts analysis threads. Two threadhandlers are started: one for Code Analysis, one for Data Analysis.
        /// </summary>
        /// <param name="repo">The repository to analyze</param>
        public void StartThread(string repo)
        {
            codeAnalysis = new ThreadHandler(CodeAnalysisProcesses(), CodeAnalysisCommands(repo), "Results" + Path.DirectorySeparatorChar + ProjectName(repo) + Path.DirectorySeparatorChar + "Code" + Path.DirectorySeparatorChar);
            dataAnalysis = new ThreadHandler(DataAnalysisProcesses(), DataAnalysisCommands(repo), "Results" + Path.DirectorySeparatorChar + ProjectName(repo) + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar);
        }
        /// <summary>
        /// Fetch the project name from the repository path.
        /// </summary>
        /// <param name="path">The repository path</param>
        /// <returns></returns>
        protected string ProjectName(string path)
        {
            string[] rep = path.Split(Path.DirectorySeparatorChar);
            return rep.ElementAt(rep.Length - 1);
        }
        /// <summary>
        /// Construct process list to run based on OS. CodeSmell case
        /// </summary>
        /// <returns>List of process</returns>
        protected List<string> CodeAnalysisProcesses()
        {
            if(os == OS.Unix) return new List<string> { "mono", "mono" };
            return new List<string> { "CSharpAnalyzer.exe", "CodeSmellAnalyzer.exe" };
        }
        /// <summary>
        /// Construct process list to run based on OS. DataSmell Case
        /// </summary>
        /// <returns>List of process</returns>
        protected List<string> DataAnalysisProcesses()
        {
            if (os == OS.Unix) return new List<string> { "mono", "mono" };
            return new List<string> { "UnityDataAnalyzer.exe", "MetaSmellAnalyzer.exe" };
        }
        /// <summary>
        /// Construct code analysis commands.
        /// </summary>
        /// <param name="repo">Repository to analyze</param>
        /// <returns></returns>
        protected List<string> CodeAnalysisCommands(string repo)
        {
            List<string> commands = new List<string>();
            string path = Path.GetFullPath(repo);
            string name = ProjectName(repo);
            WriteOutput(DateTime.Now + " Analyzing Code " + name + " Repository");
            if(os == OS.Windows)
            {
                commands.Add("-n " + name + " -p " + path + " -r Results/" + name + "/Code -v");
                commands.Add("-d Results/" + name + "/Code/CodeAnalysis.json -r Results/" + name + "/Code -c -v");

            }
            else
            {
                commands.Add("CSharpAnalyzer.exe -n " + name + " -p " + path + " -r Results/" + name + "/Code -v");
                commands.Add("CodeSmellAnalyzer.exe -d Results/" + name + "/Code/CodeAnalysis.json -r Results/" + name + "/Code -c -v");

            }
            return commands;
        }
        /// <summary>
        /// Construct data analysis commands.
        /// </summary>
        /// <param name="repo">Repository to analyze</param>
        /// <returns></returns>
        protected List<string> DataAnalysisCommands(string repo)
        {
            List<string> commands = new List<string>();
            string path = Path.GetFullPath(repo);
            string name = ProjectName(repo);
            WriteOutput(DateTime.Now + " Analyzing Data " + name + " Repository");
            if(os == OS.Windows)
            {
                commands.Add("-n " + name + " -d " + path + " -r Results/" + name + "/Data -v");
                commands.Add("-d Results/" + name + "/Data -r Results/" + name + "/Data -c -v");
            }
            else
            {
                commands.Add("UnityDataAnalyzer.exe -n " + name + " -d " + path + " -r Results/" + name + "/Data -v");
                commands.Add("MetaSmellAnalyzer.exe -d Results/" + name + "/Data -r Results/" + name + "/Data -c -v");
            }
            return commands;
        }
        /// <summary>
        /// Invoke Log Operations.
        /// </summary>
        /// <param name="s">String to log</param>
        public static void WriteOutput(string s)
        {
            Logger.Log(Logger.LogLevel.Debug, s);
        }


    }

    /// <summary>
    /// Command Line Options Wrapper
    /// </summary>
    public class Options
    {
        [Option('d', "directory", Required = true, HelpText = "Directory containing the repository to analyze")]
        public string Directory { get; set; }
        [Option('v', "verbose", Required = false, HelpText = "Displays the log on the standard output.")]
        public bool Verbose { get; set; }
    }

}