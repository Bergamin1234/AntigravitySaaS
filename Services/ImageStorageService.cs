using AntigravitySaaS.Api.Services.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AntigravitySaaS.Api.Services;

public class ImageStorageService(
    HttpClient httpClient, 
    BlobServiceClient blobServiceClient,
    IConfiguration configuration,
    ILogger<ImageStorageService> logger) : IImageStorageService
{
    private const string ContainerName = "antigravity-images";

    public async Task<string> TransferImageToPermanentStorageAsync(
        string temporaryAiUrl, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Transferindo imagem da URL da IA para Blob Storage. Usuário: {UserId}", userId);

            using var response = await httpClient.GetAsync(
                temporaryAiUrl, 
                HttpCompletionOption.ResponseHeadersRead, 
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var extension = contentType switch
            {
                "image/png" => "png",
                "image/webp" => "webp",
                _ => "jpg"
            };

            var fileName = $"generations/{userId}/{Guid.NewGuid()}.{extension}";

            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(fileName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    CacheControl = "public, max-age=31536000, immutable"
                }
            };

            await using var imageStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await blobClient.UploadAsync(imageStream, uploadOptions, cancellationToken);

            var cdnDomain = configuration["Storage:CdnBaseUrl"];
            if (!string.IsNullOrEmpty(cdnDomain))
            {
                return $"{cdnDomain.TrimEnd('/')}/{fileName}";
            }

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao persistir imagem temporária {Url} no storage.", temporaryAiUrl);
            throw new InvalidOperationException("Falha na persistência da imagem no armazenamento em nuvem.", ex);
        }
    }
}
