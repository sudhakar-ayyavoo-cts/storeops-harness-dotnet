using StoreOps.Application.Staff.Errors;
using StoreOps.Domain.Staff;

namespace StoreOps.Application.Staff;

public sealed class StaffService : IStaffService
{
    private readonly IUserRepository _userRepository;

    public StaffService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<User>> ListAsync(Guid? storeId, CancellationToken ct)
        => await _userRepository.ListAsync(storeId, ct);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _userRepository.GetByIdAsync(id, ct);

    public async Task<AuthToken> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsError();
        }

        // TODO: replace with real JWT generation when auth is wired up
        return new AuthToken
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(8),
        };
    }

    // TODO: replace with BCrypt/PBKDF2 when real auth is wired up
    private static bool VerifyPassword(string password, string hash)
        => password == hash;
}
