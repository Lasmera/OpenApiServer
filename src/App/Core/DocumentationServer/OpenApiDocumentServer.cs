using System.Collections.Generic;
using System.IO;

using ITExpert.OpenApi.Utils;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

namespace ITExpert.OpenApi.Server.Core.DocumentationServer
{
    public static class OpenApiDocumentServer
    {
        public static IApplicationBuilder UseOpenApiServer(this IApplicationBuilder app,
                                                           IEnumerable<OpenApiDocument> specs,
                                                           string contentDirectory)
        {
            Directory.CreateDirectory(contentDirectory);
            WriteSpecs(contentDirectory, specs);

            var fileProvider = new PhysicalFileProvider(contentDirectory);
            return app.UseFileServer(new FileServerOptions
                                     {
                                             EnableDirectoryBrowsing = true,
                                             DirectoryBrowserOptions = {RequestPath = ""},
                                             FileProvider = fileProvider
                                     });
        }

        private static void WriteSpecs(string dir, IEnumerable<OpenApiDocument> specs)
        {
            foreach (var spec in specs)
            {
                WriteOneSpec(spec);
            }

            void WriteOneSpec(OpenApiDocument spec)
            {
                var path = GetSpecFilePath(dir, spec.Info.GetMajorVersion(), spec.Info.Title);
                var content = OpenApiSerializer.Serialize(spec);
                
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (var writer = File.CreateText(path))
                {
                    writer.Write(content);
                }
            }
        }

        private static string GetSpecFilePath(string dir, string apiVersion, string apiTitle)
        {
            const string filename = "swagger.json";
            var ver = $"v{apiVersion}";
            var name = apiTitle.Replace(" ", "").ToLowerInvariant();
            return Path.Combine(dir, name, ver, filename);
        }
    }
}