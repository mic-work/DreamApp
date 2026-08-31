using DreamApp.Api.Common;
using DreamApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DreamApp.Api.Controllers;

/// <summary>
/// Simple photo upload endpoint for gifts. Delegates to IImageStorageService,
/// which today writes to local disk (see LocalImageStorageService) but can
/// be swapped for a cloud storage implementation without any API change.
/// </summary>
[ApiController]
[Authorize]
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private readonly IImageStorageService _imageStorageService;

    public ImagesController(IImageStorageService imageStorageService)
    {
        _imageStorageService = imageStorageService;
    }

    public class UploadResultDto
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<UploadResultDto>> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest(new ApiErrorDto("Nessun file ricevuto."));
        }

        try
        {
            var url = await _imageStorageService.SaveAsync(file, cancellationToken);
            return Ok(new UploadResultDto { ImageUrl = url });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorDto(ex.Message));
        }
    }
}
