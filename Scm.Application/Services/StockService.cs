using Microsoft.EntityFrameworkCore;
using Scm.Application.DTOs;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public sealed class StockService(ScmDbContext dbContext) : IStockService
{
    private readonly ScmDbContext _dbContext = dbContext;

    public async Task ReceiveAsync(ReceivePartDto dto, CancellationToken cancellationToken = default)
    {
        var part = await _dbContext.Parts.FirstOrDefaultAsync(p => p.Sku == dto.Sku, cancellationToken);
        if (part is null)
        {
            part = new Part
            {
                Sku = dto.Sku.Trim(),
                Title = dto.Title.Trim(),
                StockQty = dto.Qty,
                ReorderPoint = Math.Max(1, dto.Qty / 2m),
                PriceIn = dto.PriceIn,
                PriceOut = dto.PriceOut,
                Unit = dto.Unit.Trim(),
                IsActive = true
            };

            _dbContext.Parts.Add(part);
        }
        else
        {
            part.Title = dto.Title.Trim();
            part.StockQty += dto.Qty;
            part.PriceIn = dto.PriceIn;
            part.PriceOut = dto.PriceOut;
            part.Unit = dto.Unit.Trim();
            part.IsActive = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Part>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Parts
            .Where(p => p.IsActive && p.StockQty <= p.ReorderPoint)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }
}
