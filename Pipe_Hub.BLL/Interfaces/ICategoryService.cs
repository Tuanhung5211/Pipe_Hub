using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

/// <summary>
/// Service quản lý danh mục (Genre / Tag / Language).
/// Đọc danh mục: mọi user. Thay đổi (Create/Update/Delete): Admin only.
/// </summary>
public interface ICategoryService
{
    // ===== Genre =====
    Task<IReadOnlyList<GenreDto>> GetGenresAsync();
    Task<Result<GenreDto>> CreateGenreAsync(string name, string? description);
    Task<Result> UpdateGenreAsync(int id, string name, string? description);
    Task<Result> DeleteGenreAsync(int id);

    // ===== Tag =====
    Task<IReadOnlyList<TagDto>> GetTagsAsync();
    Task<Result<TagDto>> CreateTagAsync(string name);
    Task<Result> UpdateTagAsync(int id, string name);
    Task<Result> DeleteTagAsync(int id);

    // ===== Language =====
    Task<IReadOnlyList<LanguageDto>> GetLanguagesAsync();
    Task<Result<LanguageDto>> CreateLanguageAsync(string name, string? code);
    Task<Result> UpdateLanguageAsync(int id, string name, string? code);
    Task<Result> DeleteLanguageAsync(int id);
}
