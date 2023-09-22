using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication();
builder.Services.AddHttpClient<ICatalogService, CatalogServiceClient>();
builder.Services.AddHttpClient<IReviewService, ReviewServiceClient>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/products-reviews", async (HttpContext httpContext, ICatalogService catalogService, IReviewService reviewService) =>
{
    //Authorization
    //CircuitBreaker
    //Protocol translation (GOD PLEASE NO! NOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO)
    //LOGS
    var products = catalogService.GetProducts();
    var reviews = reviewService.GetReviews();

    var productsWithReviews = products.Select(product => new
    {
        product.Id,
        product.Name,
        product.Price,
        Reviews = reviews.Where(review => review.ProductId == product.Id)
    });

    httpContext.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(httpContext.Response.Body, productsWithReviews);
});

app.Run();

public interface ICatalogService
{
    List<Product> GetProducts();
}

public interface IReviewService
{
    List<Review> GetReviews();
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
