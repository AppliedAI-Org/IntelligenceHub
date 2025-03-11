using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    /// <summary>
    /// A wrapper for business logic resposnes that can be used to return data and assist with constructing responses in the controllers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class APIResponseWrapper<T>
    {
        public bool IsSuccess { get; }
        public T? Data { get; }
        public string? ErrorMessage { get; }
        public APIResponseStatusCodes StatusCode { get; }

        /// <summary>
        /// A wrapper for business logic resposnes that can be used to return data and assist with constructing responses in the controllers.
        /// </summary>
        /// <param name="isSuccess">Whether or not the request succeeded.</param>
        /// <param name="data">The data to be returned in the response body.</param>
        /// <param name="errorMessage">An error message if one was set.</param>
        /// <param name="statusCode">The status code that should be associated with the result.</param>
        private APIResponseWrapper(bool isSuccess, T? data, string? errorMessage, APIResponseStatusCodes statusCode)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
            StatusCode = statusCode;
        }

        /// <summary>
        /// A method used to construct a wrapper for a successful operation.
        /// </summary>
        /// <param name="data">The data to be returned in the response body.</param>
        /// <returns>An APIResponseWrapper with properties associated with success.</returns>
        public static APIResponseWrapper<T> Success(T data) => new(true, data, null, APIResponseStatusCodes.Ok);

        /// <summary>
        /// A method used to construct a wrapper for a failed operation.
        /// </summary>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <param name="statusCode">The status code that should be returned.</param>
        /// <returns>An APiResponseWrapper with properties associated with a failure.</returns>
        public static APIResponseWrapper<T> Failure(string errorMessage, APIResponseStatusCodes statusCode) => new(false, default, errorMessage, statusCode);

        /// <summary>
        /// A method used to construct a wrapper for a failed operation.
        /// </summary>
        /// <param name="data">Any partial data that was added, or created.</param>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <param name="statusCode">The status code that should be returned.</param>
        /// <returns>An APiResponseWrapper with properties associated with a failure.</returns>
        public static APIResponseWrapper<T> Failure(T data, string errorMessage, APIResponseStatusCodes statusCode) => new(false, data, errorMessage, statusCode);
    }
}
