using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

public interface IKnowledgeCategoryService
{
    IReadOnlyList<KnowledgeCategoryViewModel> GetAll();
    Task<KnowledgeCategoryViewModel> CreateAsync(CreateKnowledgeCategoryDto input);
    Task<KnowledgeCategoryViewModel> UpdateAsync(string id, UpdateKnowledgeCategoryDto input);
    Task DeleteAsync(string id);
    CategoryMovePreviewViewModel GetMovePreview(string id, string targetCategoryId);
}

public sealed class KnowledgeCategoryService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IKnowledgeCategoryService> logger)
    : ServiceBase<IKnowledgeCategoryService>(unitOfWork, serviceProvider, logger), IKnowledgeCategoryService
{
    private readonly IKnowledgeCategoryRepository _repository = unitOfWork.GetRepository<IKnowledgeCategoryRepository>();
    private readonly ILessonConfigRepository _lessonRepository = unitOfWork.GetRepository<ILessonConfigRepository>();
    private readonly IDocumentResourceRepository _documentRepository = unitOfWork.GetRepository<IDocumentResourceRepository>();
    private readonly IKnowledgeQnARepository _qnaRepository = unitOfWork.GetRepository<IKnowledgeQnARepository>();

    public IReadOnlyList<KnowledgeCategoryViewModel> GetAll()
        => _repository.GetByCompanyOrdered().ToList().Adapt<List<KnowledgeCategoryViewModel>>();

    public Task<KnowledgeCategoryViewModel> CreateAsync(CreateKnowledgeCategoryDto input)
    {
        var name = NormalizeName(input.Name);
        var level = 1;
        if (!string.IsNullOrEmpty(input.ParentId))
        {
            var parent = _repository.Get(input.ParentId) ?? throw GeneralException.ValidationError("ไม่พบหมวดแม่");
            if (parent.Level == 2)
            {
                throw GeneralException.ValidationError("หมวดย่อยซ้อนได้ชั้นเดียว");
            }
            level = 2;
        }
        EnsureNameAvailable(input.ParentId, name, null);

        var category = new KnowledgeCategory
        {
            Id = IdGenerator.GenerateId("kbcat"),
            CompanyId = CurrentCompanyId,
            ParentId = input.ParentId,
            Level = level,
            Name = name,
            Description = input.Description,
            SortOrder = input.SortOrder,
            IsSystemDefault = false,
            CreateBy = CurrentUserId,
            CreateDate = DateTime.UtcNow,
        };
        _repository.Add(category);
        UnitOfWork.Commit();
        return Task.FromResult(category.Adapt<KnowledgeCategoryViewModel>());
    }

    public Task<KnowledgeCategoryViewModel> UpdateAsync(string id, UpdateKnowledgeCategoryDto input)
    {
        var category = _repository.Get(id) ?? throw GeneralException.NotFound("หมวด");
        if (category.IsSystemDefault)
        {
            throw GeneralException.ValidationError("แก้ไขหมวดเริ่มต้นของระบบไม่ได้");
        }
        var name = NormalizeName(input.Name);
        EnsureNameAvailable(category.ParentId, name, category.Id);
        category.Name = name;
        category.Description = input.Description;
        category.SortOrder = input.SortOrder;
        category.UpdateBy = CurrentUserId;
        category.UpdateDate = DateTime.UtcNow;
        _repository.Update(category);
        UnitOfWork.Commit();
        return Task.FromResult(category.Adapt<KnowledgeCategoryViewModel>());
    }

    public Task DeleteAsync(string id)
    {
        var category = _repository.Get(id) ?? throw GeneralException.NotFound("หมวด");
        if (category.IsSystemDefault)
        {
            throw GeneralException.ValidationError("ลบหมวดเริ่มต้นของระบบไม่ได้");
        }

        var lessonCount = _lessonRepository.CountByCategoryId(id);
        var documentCount = _documentRepository.GetByScope(KnowledgeScopeType.Category, id).Count();
        var qnaCount = _qnaRepository.GetByScope(KnowledgeScopeType.Category, id).Count();
        var childCount = _repository.GetChildren(id).Count();
        if (lessonCount > 0 || documentCount > 0 || qnaCount > 0 || childCount > 0)
        {
            throw GeneralException.ValidationError($"ลบไม่ได้ - มีบทเรียน {lessonCount} · เอกสาร {documentCount} · Q&A {qnaCount} · หมวดย่อย {childCount} อยู่ในหมวดนี้");
        }

        category.IsDelete = true;
        category.DeleteBy = CurrentUserId;
        category.DeletedAt = DateTime.UtcNow;
        _repository.Update(category);
        UnitOfWork.Commit();
        return Task.CompletedTask;
    }

    public CategoryMovePreviewViewModel GetMovePreview(string id, string targetCategoryId)
    {
        var current = _repository.Get(id) ?? throw GeneralException.NotFound("หมวด");
        var target = _repository.Get(targetCategoryId) ?? throw GeneralException.ValidationError("ไม่พบหมวดปลายทาง");
        if (current.Level != 2 || target.Level != 2)
        {
            throw GeneralException.ValidationError("ย้ายบทเรียนได้เฉพาะระหว่างหมวดย่อย");
        }
        return new CategoryMovePreviewViewModel
        {
            LosingDocuments = _documentRepository.GetByScope(KnowledgeScopeType.Category, current.Id).Count(),
            GainingDocuments = _documentRepository.GetByScope(KnowledgeScopeType.Category, target.Id).Count(),
            LosingQnAs = _qnaRepository.GetByScope(KnowledgeScopeType.Category, current.Id).Count(),
            GainingQnAs = _qnaRepository.GetByScope(KnowledgeScopeType.Category, target.Id).Count(),
        };
    }

    private void EnsureNameAvailable(string? parentId, string name, string? excludedId)
    {
        var duplicate = _repository.GetAll().AsEnumerable().Any(category =>
            category.Id != excludedId
            && category.ParentId == parentId
            && string.Equals(category.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            throw GeneralException.ValidationError("ชื่อหมวดซ้ำภายในหมวดแม่เดียวกัน");
        }
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            throw GeneralException.ValidationError("ต้องระบุชื่อหมวด");
        }
        return normalized;
    }
}
