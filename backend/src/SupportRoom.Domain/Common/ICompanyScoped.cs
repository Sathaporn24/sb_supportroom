namespace SupportRoom.Domain.Common;

/// <summary>
/// Every row belongs to exactly one company - the organization that operates its own support
/// room (School Bright, SCB, ...). NOT the organization of the person receiving a training link:
/// that one is a plain display label on TrainingLink (RecipientOrgName), with no isolation
/// meaning at all. Mixing those two up is the easiest way to build a cross-company leak, so
/// they deliberately do not share a name.
///
/// Implementing this interface is what makes ApplicationDbContext attach a company query filter
/// to the entity - an entity that skips it is silently readable across companies.
/// </summary>
public interface ICompanyScoped
{
    string CompanyId { get; }
}
