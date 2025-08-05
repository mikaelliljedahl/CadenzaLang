using System;

namespace Cadenza.Tests.Manual
{

// Math module
namespace Math
{
    public static class MathModule
    {
        public static int add(int a, int b)
        {
            return a + b;
        }
        
        public static int multiply(int a, int b)
        {
            return a * b;
        }
        
        private static int internal_helper(int x)
        {
            return x * 2;
        }
    }
}

// Utils module
namespace Utils
{
    public static class UtilsModule
    {
        public static int Double(int x)  // Renamed to avoid 'double' keyword conflict
        {
            return x * 2;
        }

        public static int square(int x)
        {
            return x * x;
        }

        public static string greet(string name)
        {
            return string.Format("Hello, {0}!", name);
        }
    }
}

// Main program using the modules
public static class TestModulesProgram
{
    public static int calculate()
    {
        var x = MathModule.add(10, 5);        // Qualified call
        var y = UtilsModule.Double(x);        // Qualified call (renamed)
        var z = MathModule.multiply(y, 2);    // Qualified call
        return UtilsModule.square(z);         // Qualified call
    }

    public static string main()
    {
        var result = calculate();
        return UtilsModule.greet(string.Format("Result is {0}", result));
    }
    
    public static void Main(string[] args)
    {
        var result = main();
        Console.WriteLine(result);
        Console.WriteLine($"Calculate result: {calculate()}");
    }
}

}