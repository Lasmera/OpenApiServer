using System;
using System.IO;

using ITExpert.OpenApi.Server.Core.MockServer.Options;
using ITExpert.OpenApi.Server.Server;

using Microsoft.AspNetCore.Hosting;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ITExpert.OpenApi.Server.Cli.Run
{
    public class LaunchServerCommand
    {
        private LaunchServerCommandOptions Options { get; }

        public LaunchServerCommand(LaunchServerCommandOptions options)
        {
            Options = options;
        }

        public int Execute()
        {
            CreateConfig();

            var host = new WebHostFactory(Options).CreateHost();
            try
            {
                host.Start();
            }
            catch (Exception e)
            {
                PrintStartupError(e);
                return 1;
            }
            
            PrintStartupMessage();
            host.WaitForShutdown();
            PrintFinish();

            return 0;
        }

        private void PrintStartupMessage()
        {
            Console.WriteLine();
            Console.WriteLine($"OpenAPI Server is running on http://localhost:{Options.Port}".PadRight(60));
            Console.WriteLine("Press Ctrl+C to terminate.".PadRight(60));

            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine($"* Verbosity: {Options.MinLogLevel}");
            Console.WriteLine($"* Config: {Path.GetFullPath(Options.ConfigPath)}");
            Console.WriteLine($"* Sources: {string.Join(", ", Options.Sources)}");
            Console.WriteLine("".PadRight(60, '*'));
            Console.WriteLine();
        }

        private static void PrintStartupError(Exception e)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Critical startup error:");
            Console.ResetColor();
            Console.WriteLine(e.Message);
            Console.WriteLine();
            Console.WriteLine("Exiting...");
        }

        private static void PrintFinish()
        {
            Console.WriteLine();
            Console.WriteLine("Terminated.");
            Console.WriteLine();
        }

        private void CreateConfig()
        {
            var path = Options.ConfigPath;
            if (File.Exists(path))
            {
                return;
            }

            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);

            var optionsText = GetDefaultOptions();
            using (var writer = File.CreateText(path))
            {
                writer.Write(optionsText);
            }

            string GetDefaultOptions()
            {
                var settings = new JsonSerializerSettings
                               {
                                       Formatting = Formatting.Indented,
                                       ContractResolver = new CamelCasePropertyNamesContractResolver(),
                               };
                settings.Converters.Add(new StringEnumConverter(camelCaseText: true));

                var options = new MockServerOptions
                              {
                                      MockServerHost = $"http://localhost:{Options.Port}",
                                      Routes = new[] {MockServerRouteOptions.Default}
                              };
                return JsonConvert.SerializeObject(options, settings);
            }
        }
    }
}