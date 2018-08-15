using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using OpenApiServer.Core.MockServer.Context.Types;

namespace OpenApiServer.Core.MockServer.Handlers.Defaults
{
    [RequestHandler("pipeline")]
    public class PipelineRequestHandler : IRequestHandler
    {
        private IConfiguration Config { get; }
        private IRequestHandlerProvider HandlerProvider { get; }

        public PipelineRequestHandler(IConfiguration config, IRequestHandlerProvider handlerProvider)
        {
            Config = config;
            HandlerProvider = handlerProvider;
        }

        public async Task<ResponseContext> HandleAsync(RequestContext requestContext)
        {
            var prevResponse = new ResponseContext();

            var handlersConfigs = Config.GetSection("pipeline").GetChildren();
            foreach (var handlerConfig in handlersConfigs)
            {
                if (handlerConfig.GetValue("disable", false))
                {
                    continue;
                }

                var name = handlerConfig.GetValue<string>("handler");
                if (name == null)
                {
                    throw new Exception("Unable to find handler id for one of the pipeline handlers in configuration.");
                }

                var handler = HandlerProvider.GetHandler(name, handlerConfig, prevResponse);

                var currentResponse = await handler.HandleAsync(requestContext).ConfigureAwait(false);

                if (currentResponse == null)
                {
                    continue;
                }

                if (currentResponse.BreakPipeline)
                {
                    break;
                }

                prevResponse = ConcatContexts(prevResponse, currentResponse);
            }

            return prevResponse;
        }

        private static ResponseContext ConcatContexts(ResponseContext previous, ResponseContext current)
        {
            current.ContentType = current.ContentType ?? previous.ContentType;
            current.Body = current.Body ?? previous.Body;

            foreach (var previousHeader in previous.Headers)
            {
                if (current.Headers.ContainsKey(previousHeader.Key))
                {
                    continue;
                }

                current.Headers[previousHeader.Key] = previousHeader.Value;
            }

            return current;
        }
    }
}