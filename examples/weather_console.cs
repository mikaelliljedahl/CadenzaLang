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

public static class WeatherApiProgram
{
    /// <summary>
    /// Pure function - no side effects
    /// </summary>
    /// <param name="celsius">Parameter of type int</param>
    /// <returns>Returns int</returns>
    public static int celsius_to_fahrenheit(int celsius)
    {
        return celsius * 9 / 5 + 32;
    }

    /// <summary>
    /// </summary>
    /// <param name="days">Parameter of type int</param>
    /// <returns>Returns Result<int, string></returns>
    public static Result<int, string> get_weather_forecast(int days)
    {
        if (days < 1)
        {
            return Result<int, string>.Error("Days must be positive");
        }

        if (days > 10)
        {
            return Result<int, string>.Error("Cannot forecast more than 10 days");
        }

        var base_temp = 20;
        var temp_celsius = base_temp + days;
        return Result<int, string>.Ok(temp_celsius);
    }

    /// <summary>
    /// </summary>
    /// <param name="endpoint">Parameter of type string</param>
    /// <param name="days">Parameter of type int</param>
    /// <returns>Returns Result<string, string></returns>
    public static Result<string, string> weather_api(string endpoint, int days)
    {
        if (endpoint == "forecast")
        {
            var temp_result_result = get_weather_forecast(days);
            if (temp_result_result.IsError)
                return Result<string, string>.Error(temp_result_result.ErrorValue);
            var temp_result = temp_result_result.Value;

            var temp_fahrenheit = celsius_to_fahrenheit(temp_result);
            return Result<string, string>.Ok($"Temperature: {temp_result}C ({temp_fahrenheit}F)");
        }

        return Result<string, string>.Error("Unknown endpoint");
    }

    /// <summary>
    /// </summary>
    /// <returns>Returns Result<string, string></returns>
    public static Result<string, string> weather_main()
    {
        var result_result = weather_api("forecast", 5);
        if (result_result.IsError)
            return result_result;
        var result = result_result.Value;
        return Result<string, string>.Ok(result);
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("Cadenza Weather API Demo");
        Console.WriteLine("========================");

        // Test various scenarios
        var scenarios = new[]
        {
            ("forecast", 1),
            ("forecast", 5),
            ("forecast", 10),
            ("forecast", 15), // Should error
            ("forecast", -1), // Should error
            ("unknown", 5)    // Should error
        };

        foreach (var (endpoint, days) in scenarios)
        {
            Console.WriteLine($"\nTesting: {endpoint} with {days} days");
            var result = weather_api(endpoint, days);
            
            if (result.IsError)
            {
                Console.WriteLine($"❌ Error: {result.ErrorValue}");
            }
            else
            {
                Console.WriteLine($"✅ Success: {result.Value}");
            }
        }

        Console.WriteLine("\n--- Main Demo ---");
        var mainResult = weather_main();
        if (mainResult.IsError)
        {
            Console.WriteLine($"❌ Main Error: {mainResult.ErrorValue}");
        }
        else
        {
            Console.WriteLine($"✅ Main Success: {mainResult.Value}");
        }
    }
}