using JunoBank.Application.DTOs;

namespace JunoBank.Application.Interfaces;

/// <summary>
/// Service for managing first-run setup and user creation.
/// </summary>
public interface ISetupService
{
    Task<bool> IsSetupRequiredAsync();
    Task<bool> HasAdminAsync();
    Task<SetupResult> CompleteSetupAsync(SetupData data);
}
