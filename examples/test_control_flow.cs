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

public class Program
{
    public static bool test_boolean(int a, int b)
    {
        return a > 0 && b > 0;
    }

    public static Result<string, string> test_guard(int value)
    {
        if (!(value >= 0))
        {
            return Result<string, string>.Error("Negative value");
        }

        return Result<string, string>.Ok("Valid");
    }

    public static string test_if(int age)
    {
        if (age < 18)
        {
            return "Minor";
        }
        else
        {
            return "Adult";
        }
    }

    public static bool complex_boolean(int a, int b, bool flag)
    {
        return (a > 0 && b > 0) || (flag && a != b);
    }

    public static bool test_not(bool value)
    {
        return !value;
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("Testing Cadenza Control Flow:");
        
        // Test boolean expressions
        Console.WriteLine($"test_boolean(5, 3): {test_boolean(5, 3)}"); // should be true
        Console.WriteLine($"test_boolean(-1, 3): {test_boolean(-1, 3)}"); // should be false
        
        // Test guard statements
        var result1 = test_guard(10);
        Console.WriteLine($"test_guard(10): IsError={result1.IsError}, Value={result1.Value}");
        
        var result2 = test_guard(-5);
        Console.WriteLine($"test_guard(-5): IsError={result2.IsError}, ErrorValue={result2.ErrorValue}");
        
        // Test if statements
        Console.WriteLine($"test_if(15): {test_if(15)}"); // should be "Minor"
        Console.WriteLine($"test_if(25): {test_if(25)}"); // should be "Adult"
        
        // Test complex boolean expressions
        Console.WriteLine($"complex_boolean(5, 3, false): {complex_boolean(5, 3, false)}"); // should be true
        Console.WriteLine($"complex_boolean(-1, -2, true): {complex_boolean(-1, -2, true)}"); // should be true
        Console.WriteLine($"complex_boolean(-1, -2, false): {complex_boolean(-1, -2, false)}"); // should be false
        
        // Test NOT operator
        Console.WriteLine($"test_not(true): {test_not(true)}"); // should be false
        Console.WriteLine($"test_not(false): {test_not(false)}"); // should be true
    }
}