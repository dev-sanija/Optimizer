using System;

namespace Optimzer.Core
{
    public class Result
    {
        public bool Success { get; }
        public string Message { get; }

        protected Result(bool success, string? message)
        {
            Success = success;
            Message = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        }

        public static Result Ok(string? message = null)
        {
            return new Result(true, message);
        }

        public static Result Fail(string? message)
        {
            return new Result(false, message);
        }
    }

    public sealed class Result<T> : Result
    {
        public T? Data { get; }

        private Result(bool success, string? message, T? data)
            : base(success, message)
        {
            Data = data;
        }

        public static Result<T> Ok(T data, string? message = null)
        {
            return new Result<T>(true, message, data);
        }

        public static new Result<T> Fail(string? message)
        {
            return new Result<T>(false, message, default);
        }
    }
}