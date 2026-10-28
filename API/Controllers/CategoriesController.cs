
using Application.ApiResult;
using Application.DTOs.Category;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categoryService.GetAllAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        var result = await _categoryService.CreateAsync(dto);

        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.Validation =>
                    BadRequest(new { error = result.ErrorMessage }),

                ErrorType.Conflict =>
                    Conflict(new { error = result.ErrorMessage }),

                _ => BadRequest(new { error = result.ErrorMessage })
            };
        }

        return Ok(result.Data);
    }
}
