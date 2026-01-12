using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Queries.GetProducts;

public record GetProductsQuery : IRequest<List<ProductDto>>;

public record ProductDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    string? CoverImageUrl,
    string Developer,
    DateTime ReleaseDate);

public class GetProductsQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.ReleaseDate)
            .Select(p => new ProductDto(
                p.Id,
                p.Title,
                p.Description,
                p.Price,
                p.Currency,
                p.CoverImageUrl,
                p.Developer,
                p.ReleaseDate))
            .ToListAsync(cancellationToken);

        return products;
    }
}
