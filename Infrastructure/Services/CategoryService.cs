
using Application.ApiResult;
using Application.DTOs.Category;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CategoryDto>>> GetAllAsync()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();

        return Result<List<CategoryDto>>.Success(categories);
    }

    public async Task<Result<CategoryDto>> CreateAsync(
        CreateCategoryDto dto)
    {
        var name = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            name.Length < 2 || name.Length > 100)
        {
            return Result<CategoryDto>.Fail(
                "Название должно содержать от 2 до 100 символов.",
                ErrorType.Validation);
        }

        var exists = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower());

        if (exists)
        {
            return Result<CategoryDto>.Fail(
                "Такая категория уже существует.",
                ErrorType.Conflict);
        }

        var category = new Category
        {
            Name = name
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var result = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };

        return Result<CategoryDto>.Success(result);
    }
}
