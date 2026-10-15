using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WebBiblioteca.Helpers
{
    public class ImageHelper : IImageHelper
    {
        public async Task<string> UploadImageAsync(IFormFile imageFile, string folder)
        {
            string guid = Guid.NewGuid().ToString();
            string extension = imageFile.ContentType == "image/png" ? ".png" : ".jpg";
            string file = $"{guid}{extension}";

            string directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", folder);
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, file);
            string imageUrl = $"~/images/{folder}/{file}";

            try
            {
                using FileStream stream = new FileStream(path, FileMode.CreateNew);
                await imageFile.CopyToAsync(stream);
            }
            catch (IOException)
            {
                TryDeleteImage(imageUrl, folder);
                throw;
            }

            return imageUrl;
        }

        public bool TryDeleteImage(string? imageUrl, string folder)
        {
            string prefix = $"~/images/{folder}/";
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            string file = imageUrl[prefix.Length..];
            string extension = Path.GetExtension(file);
            if (!Guid.TryParse(Path.GetFileNameWithoutExtension(file), out _) ||
                Path.GetFileName(file) != file || (extension != ".jpg" && extension != ".png"))
            {
                return false;
            }

            try
            {
                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", folder, file));
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}