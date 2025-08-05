using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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

public static class WeatherHttpApi
{
    // Cadenza generated functions (cleaned up)
    public static int celsius_to_fahrenheit(int celsius)
    {
        return celsius * 9 / 5 + 32;
    }

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

    public static async Task Main(string[] args)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        
        Console.WriteLine("Cadenza Weather API Server");
        Console.WriteLine("===========================");
        Console.WriteLine("Server running at: http://localhost:8080/");
        Console.WriteLine("Try: http://localhost:8080/weather?days=5");
        Console.WriteLine("Press Ctrl+C to stop...");

        while (true)
        {
            try
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                break;
            }
        }
    }

    private static async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            Console.WriteLine($"Request: {request.HttpMethod} {request.Url?.PathAndQuery}");

            if (request.Url?.AbsolutePath == "/weather")
            {
                var query = HttpUtility.ParseQueryString(request.Url.Query);
                var daysParam = query["days"];

                if (int.TryParse(daysParam, out int days))
                {
                    var result = weather_api("forecast", days);
                    
                    if (result.IsError)
                    {
                        response.StatusCode = 400;
                        await WriteResponse(response, $"{{\"error\": \"{result.ErrorValue}\"}}");
                    }
                    else
                    {
                        response.StatusCode = 200;
                        await WriteResponse(response, $"{{\"result\": \"{result.Value}\"}}");
                    }
                }
                else
                {
                    response.StatusCode = 400;
                    await WriteResponse(response, "{\"error\": \"Invalid or missing 'days' parameter\"}");
                }
            }
            else if (request.Url?.AbsolutePath == "/")
            {
                response.StatusCode = 200;
                await WriteResponse(response, @"{
                    ""message"": ""Cadenza Weather API"",
                    ""endpoints"": {
                        ""/weather?days=N"": ""Get weather forecast for N days (1-10)""
                    }
                }");
            }
            else
            {
                response.StatusCode = 404;
                await WriteResponse(response, "{\"error\": \"Endpoint not found\"}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request error: {ex.Message}");
            response.StatusCode = 500;
            await WriteResponse(response, "{\"error\": \"Internal server error\"}");
        }
    }

    private static async Task WriteResponse(HttpListenerResponse response, string content)
    {
        response.ContentType = "application/json";
        var buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }
}