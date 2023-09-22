using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IReviewService, ReviewService>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/reviews", async (HttpContext httpContext, IReviewService reviewService) =>
{
    var products = reviewService.GetReviews();
    httpContext.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(httpContext.Response.Body, products);
});

app.Run();


public interface IReviewService
{
    List<Review> GetReviews();
}

public class ReviewService : IReviewService
{
    private readonly List<Review> _reviews = new()
    {
        new Review { ProductId = 1, Comment = "Good product", Rating = 5 },
        new Review { ProductId = 1, Comment = "Excellent!", Rating = 5 },
        new Review { ProductId = 2, Comment = "Not bad", Rating = 4 },
        new Review { ProductId = 2, Comment = "Could be better", Rating = 3 },
        // Добавьте другие отзывы
    };

    public List<Review> GetReviews()
    {
        return _reviews;
    }
}

public record Review
{
    public int ProductId { get; init; }
    public string Comment { get; init; }
    public int Rating { get; init; }
}