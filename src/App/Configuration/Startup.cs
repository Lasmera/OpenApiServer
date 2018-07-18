using System.IO;
using System.Linq;

using ITExpert.OpenApi.Server.Core.DocumentationServer;
using ITExpert.OpenApi.Server.Core.MockServer;
using ITExpert.OpenApi.Utils;

using JetBrains.Annotations;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ITExpert.OpenApi.Server.Configuration
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [UsedImplicitly]
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMockServer();
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var contentRoot = Path.Combine(env.ContentRootPath, "wwwroot");

            var defaultDir = Path.Combine(contentRoot, "specs");
            var specsDir = Configuration.GetValue("specs", defaultDir);
            var specs = OpenApiDocumentsProvider.GetDocuments(specsDir).ToArray();

            app.UseMockServer(specs)
               .UseOpenApiServer(specs, contentRoot);
        }
    }
}
