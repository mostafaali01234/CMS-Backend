using CMS.Application.DTOs;
using Microsoft.EntityFrameworkCore;
namespace CMS.Application.Helpers;

public static class PagingExtensions
{
    // counts, clamps an out-of-range page (e.g. after deleting the last row of the last page), then takes one page
    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, PagedFilterDto f)
    {
        f.Normalize();
        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)f.PageSize));
        if (f.Page > pages) f.Page = pages;

        var items = await query.Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).ToListAsync();
        return new PagedResult<T> { Items = items, Total = total, Page = f.Page, PageSize = f.PageSize };
    }
    public static PagedResult<T> PageOf<T>(this IReadOnlyList<T> all, PagedFilterDto f)
    {
        f.Normalize();
        var pages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)f.PageSize));
        if (f.Page > pages) f.Page = pages;
        return new PagedResult<T>
        {
            Items = all.Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).ToList(),
            Total = all.Count,
            Page = f.Page,
            PageSize = f.PageSize
        };
    }
}