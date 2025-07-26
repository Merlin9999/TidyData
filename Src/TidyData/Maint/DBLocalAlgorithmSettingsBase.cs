#nullable disable
using NodaTime;

namespace TidyData.Maint;

public record DBLocalAlgorithmSettingsBase
{
    public Duration MinAgeToDeleteSoftDeletedDocs { get; init; }
}