using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Services;

/// <summary>
/// Service quản lý danh mục (Genre / Tag / Language).
/// Thao tác thay đổi dữ liệu yêu cầu quyền Admin.
/// </summary>
public class CategoryService : ICategoryService
{
    private const string InUseMessage = "Danh mục đang được sử dụng, không thể xóa.";

    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public CategoryService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    /// <summary>Chặn thao tác thay đổi danh mục nếu không phải Admin.</summary>
    private void EnsureAdmin()
    {
        if (!_currentUser.IsAdmin)
        {
            throw new AppException("Chỉ Admin mới có quyền quản lý danh mục.");
        }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // =====================================================================
    // GENRE
    // =====================================================================

    public async Task<IReadOnlyList<GenreDto>> GetGenresAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var genres = await context.Genres
            .OrderBy(g => g.Name)
            .ToListAsync();

        return genres.Select(g => new GenreDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description
        }).ToList();
    }

    public async Task<Result<GenreDto>> CreateGenreAsync(string name, string? description)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<GenreDto>.Failure("Tên thể loại không được để trống.");
        }

        name = name.Trim();

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Genres.AnyAsync(g => g.Name == name))
        {
            throw new AppException($"Thể loại '{name}' đã tồn tại.");
        }

        var genre = new Genre
        {
            Name = name,
            Description = Normalize(description)
        };

        context.Genres.Add(genre);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "CreateGenre",
            details: $"Admin '{_currentUser.Username}' tạo thể loại '{genre.Name}' (Id={genre.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Genre),
            entityId: genre.Id);

        return Result<GenreDto>.Success(new GenreDto
        {
            Id = genre.Id,
            Name = genre.Name,
            Description = genre.Description
        });
    }

    public async Task<Result> UpdateGenreAsync(int id, string name, string? description)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure("Tên thể loại không được để trống.");
        }

        name = name.Trim();

        await using var context = await _contextFactory.CreateDbContextAsync();

        var genre = await context.Genres.FirstOrDefaultAsync(g => g.Id == id);

        if (genre is null)
        {
            throw new AppException($"Không tìm thấy thể loại với Id = {id}.");
        }

        if (await context.Genres.AnyAsync(g => g.Name == name && g.Id != id))
        {
            throw new AppException($"Thể loại '{name}' đã tồn tại.");
        }

        genre.Name = name;
        genre.Description = Normalize(description);
        genre.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "UpdateGenre",
            details: $"Admin '{_currentUser.Username}' cập nhật thể loại '{genre.Name}' (Id={genre.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Genre),
            entityId: genre.Id);

        return Result.Success();
    }

    public async Task<Result> DeleteGenreAsync(int id)
    {
        EnsureAdmin();

        await using var context = await _contextFactory.CreateDbContextAsync();

        var genre = await context.Genres.FirstOrDefaultAsync(g => g.Id == id);

        if (genre is null)
        {
            throw new AppException($"Không tìm thấy thể loại với Id = {id}.");
        }

        // Chặn xóa khi danh mục đang được game sử dụng.
        if (await context.Games.AnyAsync(g => g.Genres.Any(ge => ge.Id == id)))
        {
            return Result.Failure(InUseMessage);
        }

        var name = genre.Name;
        context.Genres.Remove(genre);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(InUseMessage);
        }

        _ = _logService.WriteAsync(
            action: "DeleteGenre",
            details: $"Admin '{_currentUser.Username}' xóa thể loại '{name}' (Id={id})",
            userId: _currentUser.UserId,
            entityName: nameof(Genre),
            entityId: id);

        return Result.Success();
    }

    // =====================================================================
    // TAG
    // =====================================================================

    public async Task<IReadOnlyList<TagDto>> GetTagsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var tags = await context.Tags
            .OrderBy(t => t.Name)
            .ToListAsync();

        return tags.Select(t => new TagDto
        {
            Id = t.Id,
            Name = t.Name
        }).ToList();
    }

    public async Task<Result<TagDto>> CreateTagAsync(string name)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<TagDto>.Failure("Tên thẻ không được để trống.");
        }

        name = name.Trim();

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Tags.AnyAsync(t => t.Name == name))
        {
            throw new AppException($"Thẻ '{name}' đã tồn tại.");
        }

        var tag = new Tag { Name = name };

        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "CreateTag",
            details: $"Admin '{_currentUser.Username}' tạo thẻ '{tag.Name}' (Id={tag.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Tag),
            entityId: tag.Id);

        return Result<TagDto>.Success(new TagDto
        {
            Id = tag.Id,
            Name = tag.Name
        });
    }

    public async Task<Result> UpdateTagAsync(int id, string name)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure("Tên thẻ không được để trống.");
        }

        name = name.Trim();

        await using var context = await _contextFactory.CreateDbContextAsync();

        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == id);

        if (tag is null)
        {
            throw new AppException($"Không tìm thấy thẻ với Id = {id}.");
        }

        if (await context.Tags.AnyAsync(t => t.Name == name && t.Id != id))
        {
            throw new AppException($"Thẻ '{name}' đã tồn tại.");
        }

        tag.Name = name;
        tag.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "UpdateTag",
            details: $"Admin '{_currentUser.Username}' cập nhật thẻ '{tag.Name}' (Id={tag.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Tag),
            entityId: tag.Id);

        return Result.Success();
    }

    public async Task<Result> DeleteTagAsync(int id)
    {
        EnsureAdmin();

        await using var context = await _contextFactory.CreateDbContextAsync();

        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == id);

        if (tag is null)
        {
            throw new AppException($"Không tìm thấy thẻ với Id = {id}.");
        }

        if (await context.Games.AnyAsync(g => g.Tags.Any(t => t.Id == id)))
        {
            return Result.Failure(InUseMessage);
        }

        var name = tag.Name;
        context.Tags.Remove(tag);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(InUseMessage);
        }

        _ = _logService.WriteAsync(
            action: "DeleteTag",
            details: $"Admin '{_currentUser.Username}' xóa thẻ '{name}' (Id={id})",
            userId: _currentUser.UserId,
            entityName: nameof(Tag),
            entityId: id);

        return Result.Success();
    }

    // =====================================================================
    // LANGUAGE
    // =====================================================================

    public async Task<IReadOnlyList<LanguageDto>> GetLanguagesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var languages = await context.Languages
            .OrderBy(l => l.Name)
            .ToListAsync();

        return languages.Select(l => new LanguageDto
        {
            Id = l.Id,
            Name = l.Name,
            Code = l.Code
        }).ToList();
    }

    public async Task<Result<LanguageDto>> CreateLanguageAsync(string name, string? code)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<LanguageDto>.Failure("Tên ngôn ngữ không được để trống.");
        }

        name = name.Trim();

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Languages.AnyAsync(l => l.Name == name))
        {
            throw new AppException($"Ngôn ngữ '{name}' đã tồn tại.");
        }

        var language = new Language
        {
            Name = name,
            Code = Normalize(code)
        };

        context.Languages.Add(language);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "CreateLanguage",
            details: $"Admin '{_currentUser.Username}' tạo ngôn ngữ '{language.Name}' (Id={language.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Language),
            entityId: language.Id);

        return Result<LanguageDto>.Success(new LanguageDto
        {
            Id = language.Id,
            Name = language.Name,
            Code = language.Code
        });
    }

    public async Task<Result> UpdateLanguageAsync(int id, string name, string? code)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure("Tên ngôn ngữ không được để trống.");
        }

        name = name.Trim();

        await using var context = await _contextFactory.CreateDbContextAsync();

        var language = await context.Languages.FirstOrDefaultAsync(l => l.Id == id);

        if (language is null)
        {
            throw new AppException($"Không tìm thấy ngôn ngữ với Id = {id}.");
        }

        if (await context.Languages.AnyAsync(l => l.Name == name && l.Id != id))
        {
            throw new AppException($"Ngôn ngữ '{name}' đã tồn tại.");
        }

        language.Name = name;
        language.Code = Normalize(code);
        language.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "UpdateLanguage",
            details: $"Admin '{_currentUser.Username}' cập nhật ngôn ngữ '{language.Name}' (Id={language.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Language),
            entityId: language.Id);

        return Result.Success();
    }

    public async Task<Result> DeleteLanguageAsync(int id)
    {
        EnsureAdmin();

        await using var context = await _contextFactory.CreateDbContextAsync();

        var language = await context.Languages.FirstOrDefaultAsync(l => l.Id == id);

        if (language is null)
        {
            throw new AppException($"Không tìm thấy ngôn ngữ với Id = {id}.");
        }

        if (await context.Games.AnyAsync(g => g.Languages.Any(l => l.Id == id)))
        {
            return Result.Failure(InUseMessage);
        }

        var name = language.Name;
        context.Languages.Remove(language);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(InUseMessage);
        }

        _ = _logService.WriteAsync(
            action: "DeleteLanguage",
            details: $"Admin '{_currentUser.Username}' xóa ngôn ngữ '{name}' (Id={id})",
            userId: _currentUser.UserId,
            entityName: nameof(Language),
            entityId: id);

        return Result.Success();
    }
}
