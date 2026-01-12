using Application.Features.Catalog.Queries.GetProductById;
using Application.Features.Catalog.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CatalogController(IMediator mediator) : ControllerBase
{
    [HttpGet("products")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ProductDto>>> GetProducts()
    {
        var result = await mediator.Send(new GetProductsQuery());
        return Ok(result);
    }

    [HttpGet("products/{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        var result = await mediator.Send(new GetProductByIdQuery(id));
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }
}
