using System.Globalization;
using Mapster;
using SupportRoom.Domain.Entities;
using SupportRoom.Application.ViewModel;

namespace SupportRoom.Application.Common;

/// <summary>
/// Registers the DateTime &lt;-&gt; ISO-8601 string conversions Mapster's default mapping
/// doesn't guarantee (it would otherwise use culture-dependent ToString()/Parse()), so every
/// .Adapt&lt;T&gt;() call across Services keeps entity DateTime columns and ViewModel/Dto
/// string timestamps on the exact wire format the frontend already expects.
/// </summary>
public static class MapsterConfig
{
    private static readonly object Lock = new();
    private static bool _applied;

    public static void Apply()
    {
        // Test classes each call Apply() from their constructor, and xunit runs those
        // constructors concurrently across test classes - without this guard, two threads
        // racing into TypeAdapterConfig's global registration can throw mid-compilation.
        if (_applied) return;
        lock (Lock)
        {
            if (_applied) return;

            ApplyConfig();
            _applied = true;
        }
    }

    private static void ApplyConfig()
    {
        // `string` and `string?` (likewise `DateTime` and `DateTime?` on the destination side)
        // are the SAME reflection Type at runtime - NRT annotations are compile-time-only
        // metadata, so registering both `<DateTime?, string?>` and `<DateTime?, string>`
        // silently overwrites the same config slot instead of adding two. One null-safe config
        // per real (source, destination) Type pair - never force-unwrap with `!.Value`.
        TypeAdapterConfig<DateTime, string>.NewConfig().MapWith(d => d.ToString("O", CultureInfo.InvariantCulture));
        TypeAdapterConfig<DateTime?, string>.NewConfig().MapWith(d => d == null ? null : d.Value.ToString("O", CultureInfo.InvariantCulture));
        TypeAdapterConfig<string, DateTime>.NewConfig().MapWith(s => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        TypeAdapterConfig<string, DateTime?>.NewConfig().MapWith(s => s == null ? null : DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

        // Mapster matches by property NAME - IEntityMaster's CreateDate/UpdateDate don't match
        // the wire contract's CreatedAt/UpdatedAt, so every Entity -> ViewModel pair that exposes
        // them needs an explicit override or the field silently comes back null.
        // TrainingLink and LearningSession are built by hand in their services instead of mapped -
        // both carry a field computed at read time (Status/IsStalled) that has no entity column to
        // map from, so a half-mapped, half-patched object would be the more confusing option.
        TypeAdapterConfig<SessionQuestion, SessionQuestionViewModel>.NewConfig()
            .Map(dest => dest.CreatedAt, src => src.CreateDate);
        TypeAdapterConfig<LessonConfig, LessonConfigViewModel>.NewConfig()
            .Map(dest => dest.CreatedAt, src => src.CreateDate)
            .Map(dest => dest.UpdatedAt, src => src.UpdateDate);
        TypeAdapterConfig<ChatMessage, ChatMessageViewModel>.NewConfig()
            .Map(dest => dest.CreatedAt, src => src.CreateDate);
        TypeAdapterConfig<DocumentResource, DocumentResourceViewModel>.NewConfig()
            .Map(dest => dest.CreatedAt, src => src.CreateDate);
    }
}
