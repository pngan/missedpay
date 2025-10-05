using System.ComponentModel.DataAnnotations;

namespace missedpay.ApiService.Models;

/// <summary>
/// Base entity with multi-tenancy support using UUIDv7
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Tenant identifier - UUIDv7 for time-ordered UUIDs
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
}
