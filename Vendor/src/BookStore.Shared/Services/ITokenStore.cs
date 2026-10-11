namespace BookStore.Shared.Services;

public interface ITokenStore
{
    Task<string?> GetAsync();
    Task<string?> GetRoleAsync();
    Task SetAsync(string accessToken, string role);
    Task ClearAsync();
}
