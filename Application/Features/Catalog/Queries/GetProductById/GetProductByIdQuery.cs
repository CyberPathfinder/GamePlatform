using Application.Common.Interfaces;
using Application.Features.Catalog.Queries.GetProducts; // Reuse basic DTO or separate if needed
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;

public class GetProductByIdQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            // .Include(...) if we had related data
            .FirstOrDefaultAsync(cancellationToken);

        if (product == null) return null;

        return new ProductDto(
            product.Id,
            product.Title,
            product.Description,
            product.Price,
            product.Currency,
            product.CoverImageUrl,
            product.Developer,
            product.ReleaseDate);
    }
}
