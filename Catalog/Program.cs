using System.Net;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IProductService, ProductService>();


var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/products", async (HttpContext httpContext, IProductService productService) =>
{
    var products = productService.GetProducts();
    httpContext.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(httpContext.Response.Body, products);
});

app.Run();

public interface IProductService
{
    List<Product> GetProducts();
}

public class ProductService : IProductService
{
    private readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Product 1", Price = 10.99 },
        new Product { Id = 2, Name = "Product 2", Price = 15.99 },
        // ...
    };

    public List<Product> GetProducts()
    {
        return _products;
    }
}

public record Product
{
    public int Id { get; init; }
    public string Name { get; init; }
    public double Price { get; init; }
}