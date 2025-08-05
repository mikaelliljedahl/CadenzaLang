using System;

public class Result<T, E>
{
    public T Value { get; private set; }
    public E ErrorValue { get; private set; }
    public bool IsError { get; private set; }

    public static Result<T, E> Ok(T value)
    {
        return new Result<T, E>
        {
            Value = value,
            IsError = false
        };
    }

    public static Result<T, E> Error(E error)
    {
        return new Result<T, E>
        {
            ErrorValue = error,
            IsError = true
        };
    }
}

/// <summary>
/// Pure function - no side effects
/// </summary>
/// <param name="amount">Parameter of type int</param>
/// <returns>Returns int</returns>
public static class EffectSystemDemo
{
    public static int calculate_tax(int amount)
    {
        return amount * 8 / 100;
    }

    /// <summary>
    /// Effects: Logging
    /// </summary>
    /// <param name="message">Parameter of type string</param>
    /// <returns>Returns Result<string, string></returns>
    public static Result<string, string> log_message(string message)
    {
        return Result<string, string>.Ok(message);
    }

    /// <summary>
    /// Effects: Database, Logging
    /// </summary>
    /// <param name="user_id">Parameter of type int</param>
    /// <param name="name">Parameter of type string</param>
    /// <returns>Returns Result<int, string></returns>
    public static Result<int, string> save_user(int user_id, string name)
    {
        var log_result_result = log_message("Saving user: " + name);
        if (log_result_result.IsError)
            return Result<int, string>.Error(log_result_result.ErrorValue);
        var log_result = log_result_result.Value;
        return Result<int, string>.Ok(user_id);
    }

    public static void Main()
    {
        Console.WriteLine("Effect System Demo");
        
        // Test pure function
        var tax = calculate_tax(100);
        Console.WriteLine($"Tax for $100: ${tax}");
        
        // Test function with effects
        var logResult = log_message("Test message");
        if (!logResult.IsError)
        {
            Console.WriteLine($"Logged: {logResult.Value}");
        }
        
        // Test function with multiple effects
        var saveResult = save_user(1, "John Doe");
        if (!saveResult.IsError)
        {
            Console.WriteLine($"Saved user with ID: {saveResult.Value}");
        }
        
        Console.WriteLine("Demo completed successfully!");
    }
}