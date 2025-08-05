using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cadenza.Runtime;

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

public static class CadenzaProgram
{
    /// <summary>
    /// Safely divide two integers with explicit error handling
    /// 
    /// Business Rules:
    /// - Division by zero must be prevented and return descriptive error
    /// - Both operands must be integers
    /// - Result must be exact integer division (no floating point)
    /// 
    /// Expected Outcomes:
    /// - Returns quotient wrapped in Ok on successful division
    /// - Returns descriptive error message wrapped in Error for division by zero
    /// </summary>
    /// <param name="a">Parameter of type int</param>
    /// <param name="b">Parameter of type int</param>
    /// <returns>Returns Result<int, string></returns>
    public static Result<int, string> divide(int a, int b)
    {
        if (b == 0)
        {
            return Result.Error<int, string>("Division by zero");
        }

        return Result.Ok<int, string>(a / b);
    }

    /// <summary>
    /// Calculate double the result of a division operation with error propagation
    /// 
    /// Business Rules:
    /// - Must use the divide function for the division operation
    /// - Error from division should be automatically propagated using ? operator
    /// - Result should be doubled only if division succeeds
    /// 
    /// Expected Outcomes:
    /// - Returns doubled division result wrapped in Ok on success
    /// - Returns original division error if division fails
    /// </summary>
    /// <param name="x">Parameter of type int</param>
    /// <param name="y">Parameter of type int</param>
    /// <returns>Returns Result<int, string></returns>
    public static Result<int, string> calculate(int x, int y)
    {
        var result = !divide(x, y).IsSuccess ? throw new InvalidOperationException(divide(x, y).Error) : divide(x, y).Value;
        return Result.Ok<int, string>(result * 2);
    }
}