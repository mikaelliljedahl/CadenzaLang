using Cadenza.Modules.Math;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cadenza.Modules.Math
{
    public static class Math
    {
        /// <param name="a">Parameter of type int</param>
        /// <param name="b">Parameter of type int</param>
        /// <returns>Returns int</returns>
        public static int add(int a, int b)
        {
            return a + b;
        }

        /// <param name="a">Parameter of type int</param>
        /// <param name="b">Parameter of type int</param>
        /// <returns>Returns int</returns>
        public static int multiply(int a, int b)
        {
            return a * b;
        }

        /// <param name="a">Parameter of type int</param>
        /// <param name="b">Parameter of type int</param>
        /// <returns>Returns int</returns>
        public static int subtract(int a, int b)
        {
            return a - b;
        }
    }
}

public struct Result<T, E>
{
    public readonly bool IsSuccess;
    public readonly T Value;
    public readonly E Error;
    public Result(bool isSuccess, T value, E error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
}

public static class Result
{
    public static Result<T, E> Ok<T, E>(T value)
    {
        return new Result<T, E>(true, value, default);
    }

    public static Result<T, E> Error<T, E>(E error)
    {
        return new Result<T, E>(false, default, error);
    }
}

public struct Option<T>
{
    public readonly bool HasValue;
    public readonly T Value;
    public Option(bool hasValue, T value)
    {
        HasValue = hasValue;
        Value = value;
    }
}

public static class Option
{
    public static Option<T> Some<T>(T value)
    {
        return new Option<T>(true, value);
    }

    public static Option<T> None<T>()
    {
        return new Option<T>(false, default);
    }
}