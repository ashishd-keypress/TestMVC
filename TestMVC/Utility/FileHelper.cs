using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Utility
{
    public static class FileHelper
    {
        private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

        /// <summary>
        /// Validates an uploaded image file.
        /// </summary>
        public static (bool IsValid, string? ErrorMessage) ValidateImage(IFormFile? file, bool isRequired)
        {
            if (file == null || file.Length == 0)
            {
                if (isRequired)
                {
                    return (false, "Please upload an image. Image is required.");
                }
                return (true, null);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return (false, "Invalid image format. Allowed formats are: JPG, JPEG, PNG, WEBP, GIF, SVG.");
            }

            if (file.Length > MaxFileSizeInBytes)
            {
                return (false, "Image file size cannot exceed 5 MB.");
            }

            return (true, null);
        }

        /// <summary>
        /// Saves an uploaded image file into wwwroot/uploads/{subfolder} and returns its web-accessible relative path.
        /// </summary>
        public static async Task<string> SaveImageAsync(IFormFile file, IWebHostEnvironment env, string subfolder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File cannot be empty.", nameof(file));
            }

            string uploadsDir = Path.Combine(env.WebRootPath, "uploads", subfolder);
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string uniqueFileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}{extension}";
            string fullPath = Path.Combine(uploadsDir, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{subfolder}/{uniqueFileName}";
        }

        /// <summary>
        /// Safely deletes an existing uploaded file from wwwroot if it exists.
        /// </summary>
        public static void DeleteImage(string? relativePath, IWebHostEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            try
            {
                // Only allow deleting files inside /uploads/ to prevent arbitrary file deletion
                string normalized = relativePath.TrimStart('~').TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                if (!normalized.StartsWith("uploads" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string fullPath = Path.Combine(env.WebRootPath, normalized);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
                // Suppress file delete errors to not block main operation
            }
        }
    }
}
