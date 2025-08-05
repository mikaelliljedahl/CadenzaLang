using System;

class TestTwoFunctions
{
    public static int calculate_total(int unit_price, int quantity)
    {
        return unit_price * quantity;
    }

    public static int calculate_subtotal(int item1, int item2)
    {
        return item1 + item2;
    }

    static void Main()
    {
        Console.WriteLine($"calculate_total(10, 5) = {calculate_total(10, 5)}");
        Console.WriteLine($"calculate_subtotal(20, 30) = {calculate_subtotal(20, 30)}");
    }
}