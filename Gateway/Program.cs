using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.HighPerformance.Helpers;
using Polly;
using Polly.CircuitBreaker;
using Stl;

var builder = WebApplication.CreateBuilder(args);

//Found it easier to implement, than write a sample 
var circuitBreakerPolicy = Policy.Handle<TransientException>()
    .CircuitBreaker(exceptionsAllowedBeforeBreaking: 3, durationOfBreak: TimeSpan.FromSeconds(10));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<ICatalogService, CatalogServiceClient>();
builder.Services.AddHttpClient<IAggregatorService, AggrtegatorServiceClient>();
builder.Services.AddHttpClient<IReviewService, ReviewServiceClient>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/gateway",
    async (HttpContext httpContext, ICatalogService catalogService, IReviewService reviewService, IAggregatorService aggregatorService) =>
    {
        //Imagine that with some netrunner magic, we also redirect all to here
        try
        {
            //Some autorization
            httpContext.Request.Headers.Add("JWT", "Fake-token");
            httpContext.Response.Headers.Add("JWT", "Fake-token");
            var temp = await JsonSerializer.DeserializeAsync<DTObject>(httpContext.Request.Body);
            Console.WriteLine(temp.command);
            //Sample routing (should be in params, but all went wrong)
            if (temp.command=="catalog")
            {
                httpContext.Response.Redirect("/gateway/catalog");
                //catalogService.GetProducts();
            }else if (temp.command=="reviews")
            {
                httpContext.Response.Redirect("/gateway/reviews");
                //reviewService.GetReviews();
            }else if (temp.command=="aggregation")
            {
                httpContext.Response.Redirect("/gateway/aggregation");
                //aggregatorService.GetAggregation();
            }
        }
        catch (BrokenCircuitException)
        {
            Console.WriteLine(
                "The circuit breaker tripped and is temporarily disallowing requests. Will wait before trying again");
            await Task.Delay(TimeSpan.FromSeconds(15));
        }
        catch (TransientException)
        {
            Console.WriteLine("Transient exception while sending request. Will try again.");
        }

        //What it all actually about:

        //Protocol translation (GOD PLEASE NO! NOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO)
        //Load balancing
    });

app.MapGet("/gateway/reviews",
    async (HttpContext httpContext, IReviewService reviewService) =>
    {
        httpContext.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, reviewService.GetReviews());
    });


app.MapGet("/gateway/catalog",
    async (HttpContext httpContext, ICatalogService catalogService) =>
    {
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, catalogService.GetProducts());
    });
app.MapGet("/gateway/catalog-reviews",
    async (HttpContext httpContext, IAggregatorService aggregationService) =>
    {
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, aggregationService.GetAggregation());
    });

app.Run();

public interface IAggregatorService
{
    List<ProductReviews> GetAggregation();
}

public interface ICatalogService
{
    List<Product> GetProducts();
}

public interface IReviewService
{
    List<Review> GetReviews();
}

public class AggrtegatorServiceClient : IAggregatorService
{
    private readonly HttpClient _httpClient;

    public AggrtegatorServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public  List<ProductReviews> GetAggregation()
    {
        // Отправить HTTP-запрос к микросервису CatalogService и получить список продуктов
        var response = _httpClient.GetStringAsync("http://localhost:5000/products-reviews").Result;
        var productReviews = JsonSerializer.Deserialize<List<ProductReviews>>(response);
        return productReviews;
    }
}

public class CatalogServiceClient : ICatalogService
{
    private readonly HttpClient _httpClient;

    public CatalogServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public List<Product> GetProducts()
    {
        // Отправить HTTP-запрос к микросервису CatalogService и получить список продуктов
        var response = _httpClient.GetStringAsync("http://localhost:5001/products").Result;
        var products = JsonSerializer.Deserialize<List<Product>>(response);
        return products;
    }
}

public class ReviewServiceClient : IReviewService
{
    private readonly HttpClient _httpClient;

    public ReviewServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public List<Review> GetReviews()
    {
        // Отправить HTTP-запрос к микросервису ReviewService и получить список отзывов
        var response = _httpClient.GetStringAsync("http://localhost:5002/reviews").Result;
        var reviews = JsonSerializer.Deserialize<List<Review>>(response);
        return reviews;
    }
}

public record Review
{
    public int ProductId { get; init; }
    public string Comment { get; init; }
    public int Rating { get; init; }
}

public record Product
{
    public int Id { get; init; }
    public string Name { get; init; }
    public double Price { get; init; }
}

public record ProductReviews
{
    public int Id { get; init; }
    public string Name { get; init; }
    public double Price { get; init; }
    public List<Review> Reviews { get; init; }
}

public class DTObject
{
    public string command { get; init; }
}