namespace Domain.Catalog;

public class Product
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = "USD";

    public string? CoverImageUrl { get; set; }

    public string Developer { get; set; } = string.Empty;

    public DateTime ReleaseDate { get; set; }

    public bool IsActive { get; set; } = true;
}
