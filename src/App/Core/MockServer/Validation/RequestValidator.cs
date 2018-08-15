using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json.Linq;

using OpenApiServer.Core.MockServer.Context;
using OpenApiServer.Core.MockServer.Context.Types;
using OpenApiServer.Core.MockServer.Validation.Internals;
using OpenApiServer.Core.MockServer.Validation.Types;

namespace OpenApiServer.Core.MockServer.Validation
{
    public class RequestValidator : IRequestValidator
    {
        public RequestValidationStatus Validate(RequestContext context)
        {
            var operation = context.Spec;
            var paramtersErrors = operation.Parameters?.SelectMany(ValidateParameter) ??
                                  Enumerable.Empty<RequestValidationError>();

            var bodyErrors = operation.Bodies.Count > 0
                                     ? ValidateBody(context.GetBodySpec(),
                                                    context.Request.Body,
                                                    context.Request.ContentType)
                                     : Enumerable.Empty<RequestValidationError>();

            var errors = paramtersErrors.Concat(bodyErrors).ToArray();
            return new RequestValidationStatus(errors);

            IEnumerable<RequestValidationError> ValidateParameter(RequestContextParameter x)
            {
                switch (x.In)
                {
                    case ParameterLocation.Query:
                        return ValidateQuery(x, context.Request.Query);
                    case ParameterLocation.Header:
                        return ValidateHeaders(x, context.Request.Headers);
                    case ParameterLocation.Path:
                        return ValidatePath(x, context.Request.Route);
                    case ParameterLocation.Cookie:
                        return ValidateCookie(x);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static IEnumerable<RequestValidationError> ValidateHeaders(
                RequestContextParameter parameter,
                IHeaderDictionary headers)
        {
            yield break;
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static IEnumerable<RequestValidationError> ValidateCookie(RequestContextParameter parameter)
        {
            yield break;
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static IEnumerable<RequestValidationError> ValidatePath(
                RequestContextParameter parameter,
                RouteData routeData)
        {
            yield break;
        }

        private static IEnumerable<RequestValidationError> ValidateQuery(
                RequestContextParameter parameter,
                IQueryCollection query)
        {
            var hasParameter = query.TryGetValue(parameter.Name, out var parameterValues);
            if (!hasParameter)
            {
                if (parameter.Required)
                {
                    yield return ValidationError.ParameterRequired(parameter.Name);
                }

                yield break;
            }

            if (parameterValues.Count == 0 && !parameter.AllowEmptyValue)
            {
                yield return ValidationError.ParameterMustHaveValue(parameter.Name);

                yield break;
            }

            var value = parameter.GetValue(parameterValues);
            var schemaErrors = parameter.Schema.ValidateValue(value).ToArray();
            if (schemaErrors.Any())
            {
                yield return ValidationError.InvalidParameter(parameter.Name, schemaErrors);
            }
        }

        private static IEnumerable<RequestValidationError> ValidateBody(
                RequestContextBody body,
                JToken bodyContent,
                string contentType)
        {
            if (body == null)
            {
                yield return ValidationError.UnexpectedContentType(contentType);
                yield break;
            }

            if (bodyContent == null && body.Required)
            {
                yield return ValidationError.BodyRequired();
                yield break;
            }

            var schemaErrors = body.Schema.ValidateValue(bodyContent).ToArray();
            if (schemaErrors.Any())
            {
                yield return ValidationError.InvalidBody(schemaErrors);
            }
        }
    }
}