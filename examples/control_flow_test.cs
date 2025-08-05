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
    /// <param name="age">Parameter of type int</param>
    /// <returns>Returns Result<string, string></returns>
    public static Result<string, string> validate_age(int age)
    {
        if (!(age >= 0))
        {
            return Result.Error<string, string>("Age cannot be negative");
        }

        if (age < 18)
        {
            return Result.Ok<string, string>("Minor");
        }
        else
        {
            if (age < 65)
            {
                return Result.Ok<string, string>("Adult");
            }
            else
            {
                return Result.Ok<string, string>("Senior");
            }
        }
    }

    /// <param name="name">Parameter of type string</param>
    /// <param name="age">Parameter of type int</param>
    /// <param name="email">Parameter of type string</param>
    /// <returns>Returns bool</returns>
    public static bool is_valid_user(string name, int age, string email)
    {
        return name != "" && age > 0 && age < 150 && email != "";
    }

    /// <param name="a">Parameter of type int</param>
    /// <param name="b">Parameter of type int</param>
    /// <param name="c">Parameter of type bool</param>
    /// <returns>Returns bool</returns>
    public static bool complex_logic(int a, int b, bool c)
    {
        return a > 0 && b > 0 || c && a != b;
    }

    /// <param name="user_level">Parameter of type int</param>
    /// <param name="is_admin">Parameter of type bool</param>
    /// <param name="resource_level">Parameter of type int</param>
    /// <returns>Returns Result<string, string></returns>
    public static Result<string, string> check_permissions(int user_level, bool is_admin, int resource_level)
    {
        if (!(user_level >= 0 && resource_level >= 0))
        {
            return Result.Error<string, string>("Invalid levels");
        }

        if (is_admin)
        {
            return Result.Ok<string, string>("Full access");
        }
        else
        {
            if (user_level >= resource_level)
            {
                return Result.Ok<string, string>("Access granted");
            }
            else
            {
                return Result.Error<string, string>("Access denied");
            }
        }
    }

    /// <param name="value">Parameter of type string</param>
    /// <returns>Returns bool</returns>
    public static bool is_not_empty(string value)
    {
        return !value == "";
    }

    /// <param name="a">Parameter of type int</param>
    /// <param name="b">Parameter of type int</param>
    /// <returns>Returns Result<int, string></returns>
    public static Result<int, string> divide(int a, int b)
    {
        if (!(a >= 0))
        {
            return Result.Error<int, string>("First number must be non-negative");
        }

        if (!(b != 0))
        {
            return Result.Error<int, string>("Cannot divide by zero");
        }

        return Result.Ok<int, string>(a / b);
    }

    /// <param name="min">Parameter of type int</param>
    /// <param name="max">Parameter of type int</param>
    /// <param name="value">Parameter of type int</param>
    /// <returns>Returns bool</returns>
    public static bool validate_range(int min, int max, int value)
    {
        return min <= max && value >= min && value <= max;
    }
}