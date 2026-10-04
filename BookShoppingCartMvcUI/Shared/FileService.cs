namespace BookShoppingCartMvcUI.Shared;

public interface IFileService
{
    void DeleteFile(string fileName);
    Task<string> SaveFile(IFormFile file, IReadOnlyList<string> allowedExtensions);
}

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveFile(IFormFile file, IReadOnlyList<string> allowedExtensions)
    {
        var wwwPath = _environment.WebRootPath;
        var path = Path.Combine(wwwPath, "images");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Only {string.Join(",", allowedExtensions)} files allowed");
        }
        if (!ImageSignature.Matches(file))
        {
            throw new InvalidOperationException("The file is not a real image.");
        }
        string fileName = $"{Guid.NewGuid()}{extension}";
        string fileNameWithPath = Path.Combine(path, fileName);
        using var stream = new FileStream(fileNameWithPath, FileMode.Create);
        await file.CopyToAsync(stream);
        return fileName;
    }

    public void DeleteFile(string fileName)
    {
        // only allow a plain file name inside images/, no paths (stops ../ traversal)
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName) || fileName.Contains(".."))
            throw new InvalidOperationException($"'{fileName}' is not an image file name.");
        var wwwPath = _environment.WebRootPath;
        var fileNameWithPath = Path.Combine(wwwPath, "images", fileName);
        if (!File.Exists(fileNameWithPath))
            throw new FileNotFoundException(fileName);
        File.Delete(fileNameWithPath);

    }
}
