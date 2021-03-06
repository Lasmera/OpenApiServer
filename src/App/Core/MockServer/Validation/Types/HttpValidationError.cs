using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenApiServer.Core.MockServer.Validation.Types
{
    public class HttpValidationError : IEquatable<HttpValidationError>
    {
        public string Code { get; }
        public string Description { get; }
        public string Parameter { get; }
        public IEnumerable<HttpValidationError> Inner { get; }

        public HttpValidationError(string code,
                                      string description,
                                      params HttpValidationError[] inner)
                : this(code, description, null, inner)
        {
        }

        public HttpValidationError(string code,
                                      string description,
                                      string parameter,
                                      params HttpValidationError[] inner)
        {
            Code = code;
            Description = description;
            Parameter = parameter;
            Inner = inner;
        }

        public bool Equals(HttpValidationError other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            var isErrorsEqual = Inner == null && other.Inner == null ||
                                Inner != null && other.Inner != null &&
                                (
                                    Inner.Equals(other.Inner) ||
                                    Inner.SequenceEqual(other.Inner)
                                );

            return string.Equals(Code, other.Code) && string.Equals(Description, other.Description) &&
                   string.Equals(Parameter, other.Parameter) && isErrorsEqual;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((HttpValidationError)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Code != null ? Code.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Parameter != null ? Parameter.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Inner != null ? Inner.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}