using Cadenza.Modules.UserModule;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

CadenzaProgram.main();
namespace Cadenza.Modules.UserModule
{
    public static class UserModule
    {
        public class UserProfile
        {
            public int userId { get; set; }
            public string displayName { get; set; }
            public bool verified { get; set; }
        }

        /// <summary>
        /// Pure function - no side effects
        /// </summary>
        /// <param name="profile">Parameter of type UserProfile</param>
        /// <returns>Returns bool</returns>
        public static bool isValid(UserProfile profile)
        {
            return profile.displayName != "";
        }
    }
}

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

public class User
{
    public int id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
    public bool isActive { get; set; }
}

public class Product
{
    public string productId { get; set; }
    public int price { get; set; }
}

public static class CadenzaProgram
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns>Returns int</returns>
    public static int main()
    {
        return 0;
    }
}