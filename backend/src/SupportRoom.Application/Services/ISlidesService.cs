using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Domain;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Slides;

namespace SupportRoom.Application.Services;

public interface ISlidesService
{
    Task<ResolvedPresentation> ResolveAsync(ResolveSlidesDto input);
    Task<SlidesLessonContent> GetContentAsync(string presentationId);
}

public sealed class SlidesService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<ISlidesService> logger,
    ISlidesProvider slidesProvider)
    : ServiceBase<ISlidesService>(unitOfWork, serviceProvider, logger), ISlidesService
{
    public async Task<ResolvedPresentation> ResolveAsync(ResolveSlidesDto input)
    {
        try
        {
            return await slidesProvider.ResolvePresentationAsync(new ResolvePresentationInput
            {
                SlidesSourceUrl = input.SlidesSourceUrl,
                SlidesEmbedUrl = input.SlidesEmbedUrl,
            });
        }
        catch (Exception ex)
        {
            throw GeneralException.UpstreamError(ex.Message);
        }
    }

    public async Task<SlidesLessonContent> GetContentAsync(string presentationId)
    {
        try
        {
            return await slidesProvider.GetLessonContentAsync(new GetLessonContentInput { PresentationId = presentationId });
        }
        catch (Exception ex)
        {
            throw GeneralException.UpstreamError(ex.Message);
        }
    }
}
