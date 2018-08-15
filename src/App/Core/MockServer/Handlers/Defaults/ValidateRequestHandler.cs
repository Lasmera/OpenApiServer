using System.Net;
using System.Threading.Tasks;

using Newtonsoft.Json;

using OpenApiServer.Core.MockServer.Context.Types;
using OpenApiServer.Core.MockServer.Validation;
using OpenApiServer.Core.MockServer.Validation.Types;

namespace OpenApiServer.Core.MockServer.Handlers.Defaults
{
    [RequestHandler("validateRequest")]
    public class ValidateRequestHandler : IRequestHandler
    {
        private IRequestValidator RequestValidator { get; }

        public ValidateRequestHandler(IRequestValidator requestValidator)
        {
            RequestValidator = requestValidator;
        }

        public Task<ResponseContext> HandleAsync(RequestContext context)
        {
            var requestValidationStatus = RequestValidator.Validate(context);

            return requestValidationStatus.IsSuccess
                           ? Task.FromResult((ResponseContext)null)
                           : Task.FromResult(Error(requestValidationStatus));
        }

        private static ResponseContext Error(
                RequestValidationStatus validationStatus) =>
                new ResponseContext
                {
                        BreakPipeline = true,
                        StatusCode = HttpStatusCode.BadRequest,
                        ContentType = "application/json",
                        Body = JsonConvert.SerializeObject(validationStatus)
                };
    }
}