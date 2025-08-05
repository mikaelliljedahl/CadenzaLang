using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Result<T, E>
{
    public readonly bool IsSuccess;
    public readonly T Value;
    public readonly E Error;
    public bool IsError
    {
        get
        {
            return !IsSuccess;
        }
    }

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
    /// 
    /// </summary>
    /// <param name="a">Parameter of type int</param>
    /// <returns>Returns int</returns>
    public static int test(int a)
    {
        return a;
    }
}