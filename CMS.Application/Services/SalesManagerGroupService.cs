// Services/SalesManagerGroupService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Services;

public class SalesManagerGroupService : ISalesManagerGroupService
{
    private readonly IAppDbContext _db;
    private const int NotesMaxLength = 2000;
    public SalesManagerGroupService(IAppDbContext db) { _db = db; }

    private static SalesManagerGroupDto ToDto(SalesManagerGroup g) => new()
    {
        Id = g.Id,
        ManagerId = g.ManagerId,
        ManagerName = g.Manager?.Name ?? "",
        Percent = g.Percent,
        Active = g.Active,
        Notes = g.Notes,
        Sellers = g.Sellers.Where(s => !s.IsDeleted)
            .Select(s => new GroupMemberDto { Id = s.EmployeeId, Name = s.Employee?.Name ?? "" }).ToList(),
        Products = g.Products.Where(p => !p.IsDeleted)
            .Select(p => new GroupMemberDto { Id = p.ProductId, Name = p.Product?.Name ?? "" }).ToList(),
    };

    private static IQueryable<SalesManagerGroup> WithIncludes(IQueryable<SalesManagerGroup> q) => q
        .Include(g => g.Manager)
        .Include(g => g.Sellers.Where(s => !s.IsDeleted)).ThenInclude(s => s.Employee)
        .Include(g => g.Products.Where(p => !p.IsDeleted)).ThenInclude(p => p.Product);

    private async Task<string?> ValidateAsync(SalesManagerGroupDto dto, long? currentId = null)
    {
        if (dto is null) return "Group payload is required";
        if (dto.ManagerId <= 0) return "ManagerId is required";
        if (!await _db.Employee.AnyAsync(e => e.Id == dto.ManagerId && !e.IsDeleted))
            return $"Manager (Employee) with Id {dto.ManagerId} was not found";
        if (dto.Percent <= 0 || dto.Percent > 100) return "Percent must be greater than 0 and at most 100";
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        var sellerIds = (dto.Sellers ?? new()).Select(s => s.Id).ToList();
        var productIds = (dto.Products ?? new()).Select(p => p.Id).ToList();
        if (sellerIds.Count == 0) return "Add at least one seller";
        if (productIds.Count == 0) return "Add at least one product";
        if (sellerIds.Distinct().Count() != sellerIds.Count) return "A seller appears more than once";
        if (productIds.Distinct().Count() != productIds.Count) return "A product appears more than once";
        if (sellerIds.Contains(dto.ManagerId)) return "The manager cannot be one of his own sellers";

        var gid = currentId ?? 0;
        if (await _db.SalesManagerGroup.AnyAsync(g => !g.IsDeleted && g.ManagerId == dto.ManagerId && g.Id != gid))
            return "This manager already has a group. Edit it instead";

        if (await _db.Employee.CountAsync(e => sellerIds.Contains(e.Id) && !e.IsDeleted) != sellerIds.Count)
            return "One or more sellers were not found";
        if (await _db.Product.CountAsync(p => productIds.Contains(p.Id) && !p.IsDeleted) != productIds.Count)
            return "One or more products were not found";

        var taken = await _db.SalesManagerGroupSeller
            .Where(m => !m.IsDeleted && !m.Group!.IsDeleted && m.GroupId != gid && sellerIds.Contains(m.EmployeeId))
            .Select(m => new { Seller = m.Employee!.Name, Manager = m.Group!.Manager!.Name })
            .FirstOrDefaultAsync();
        if (taken != null) return $"Seller '{taken.Seller}' is already in the group of '{taken.Manager}'";

        return null;
    }

    public async Task<ServiceResult<List<SalesManagerGroupDto>>> GetAllAsync()
    {
        var list = await WithIncludes(_db.SalesManagerGroup.Where(g => !g.IsDeleted))
            .AsSplitQuery().AsNoTracking().OrderBy(g => g.Manager!.Name).ToListAsync();
        return ServiceResult<List<SalesManagerGroupDto>>.Ok(list.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<SalesManagerGroupDto?>> GetByIdAsync(long id)
    {
        var g = await WithIncludes(_db.SalesManagerGroup.Where(x => !x.IsDeleted && x.Id == id))
            .AsSplitQuery().AsNoTracking().FirstOrDefaultAsync();
        return g is null ? ServiceResult<SalesManagerGroupDto?>.NotFound("Group not found")
                         : ServiceResult<SalesManagerGroupDto?>.Ok(ToDto(g));
    }

    public async Task<ServiceResult<SalesManagerGroupDto>> CreateAsync(SalesManagerGroupDto dto)
    {
        var error = await ValidateAsync(dto);
        if (error is not null) return ServiceResult<SalesManagerGroupDto>.Failed(error);

        var g = new SalesManagerGroup
        {
            ManagerId = dto.ManagerId,
            Percent = dto.Percent,
            Active = dto.Active,
            Notes = dto.Notes ?? "",
            Sellers = dto.Sellers.Select(s => new SalesManagerGroupSeller { EmployeeId = s.Id }).ToList(),
            Products = dto.Products.Select(p => new SalesManagerGroupProduct { ProductId = p.Id }).ToList(),
        };
        _db.SalesManagerGroup.Add(g);
        await _db.SaveChangesAsync();

        var created = await WithIncludes(_db.SalesManagerGroup.Where(x => x.Id == g.Id)).AsSplitQuery().AsNoTracking().FirstAsync();
        return ServiceResult<SalesManagerGroupDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, SalesManagerGroupDto dto)
    {
        if (dto is null) return ServiceResult<bool>.Failed("Group payload is required");
        if (id != dto.Id) return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var g = await _db.SalesManagerGroup.Include(x => x.Sellers).Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (g is null) return ServiceResult<bool>.NotFound("Group not found");

        var error = await ValidateAsync(dto, id);
        if (error is not null) return ServiceResult<bool>.Failed(error);

        var now = DateTime.UtcNow;
        g.ManagerId = dto.ManagerId; g.Percent = dto.Percent; g.Active = dto.Active; g.Notes = dto.Notes ?? "";

        // sellers and products: soft-delete the removed ones, add the new ones, keep the rest
        var sellerIds = dto.Sellers.Select(s => s.Id).ToHashSet();
        foreach (var s in g.Sellers.Where(s => !s.IsDeleted && !sellerIds.Contains(s.EmployeeId)))
        { s.IsDeleted = true; s.DeletedAtUtc = now; }
        var haveSellers = g.Sellers.Where(s => !s.IsDeleted).Select(s => s.EmployeeId).ToHashSet();
        foreach (var sid in sellerIds.Where(x => !haveSellers.Contains(x)))
            g.Sellers.Add(new SalesManagerGroupSeller { GroupId = g.Id, EmployeeId = sid });

        var productIds = dto.Products.Select(p => p.Id).ToHashSet();
        foreach (var p in g.Products.Where(p => !p.IsDeleted && !productIds.Contains(p.ProductId)))
        { p.IsDeleted = true; p.DeletedAtUtc = now; }
        var haveProducts = g.Products.Where(p => !p.IsDeleted).Select(p => p.ProductId).ToHashSet();
        foreach (var pid in productIds.Where(x => !haveProducts.Contains(x)))
            g.Products.Add(new SalesManagerGroupProduct { GroupId = g.Id, ProductId = pid });

        await _db.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var g = await _db.SalesManagerGroup.Include(x => x.Sellers).Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (g is null) return ServiceResult<bool>.NotFound("Group not found");

        var now = DateTime.UtcNow;
        g.IsDeleted = true; g.DeletedAtUtc = now;
        foreach (var s in g.Sellers.Where(s => !s.IsDeleted)) { s.IsDeleted = true; s.DeletedAtUtc = now; }
        foreach (var p in g.Products.Where(p => !p.IsDeleted)) { p.IsDeleted = true; p.DeletedAtUtc = now; }

        await _db.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }
}