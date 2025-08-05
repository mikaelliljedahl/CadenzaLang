using System;

class TestProgram
{
    public static int add(int a, int b)
    {
        return a + b;
    }

    public static int multiply(int x, int y)
    {
        return x * y;
    }

    static void Main()
    {
        Console.WriteLine($"add(5, 3) = {add(5, 3)}");
        Console.WriteLine($"multiply(4, 6) = {multiply(4, 6)}");
    }
}