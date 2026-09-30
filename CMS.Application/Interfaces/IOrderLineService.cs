using CMS.Domain.Models;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IOrderLineService
{
    Task<ServiceResult<List<OrderLine>>> GetAllAsync();
    Task<ServiceResult<OrderLine?>> GetByIdAsync(long id);
    Task<ServiceResult<OrderLine>> CreateAsync(OrderLine line);
    Task<ServiceResult<bool>> AddCitiesAsync(long id, List<long> citiesIds);
    Task<ServiceResult<bool>> AddCategoriesAsync(long id, List<long> categoriesIds);
    Task<ServiceResult<bool>> UpdateAsync(long id, OrderLine line);
    Task<ServiceResult<bool>> DeleteAsync(long id);
}