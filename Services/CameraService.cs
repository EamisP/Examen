

using CommunityToolkit.Maui.Media;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Examen.Services
{
    public class CameraService
    {
        public async Task<string> TomarFotoComoBase64Async()
        {
            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();

                if (photo == null)
                    return null;

                using var stream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                var imageBytes = memoryStream.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                // Puedes loggear o manejar la excepción aquí
                return null;
            }
        }
    }
}