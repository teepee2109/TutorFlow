using System.Net.Http.Json;
using System.Text.Json;
using TutorFlow.Web.Models;

namespace TutorFlow.Web.Services.Api;

public sealed class TutorFlowApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public TutorFlowApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<ApiResponse<LoginResponseDto>> LoginAsync(string email, string password, bool rememberMe) =>
        SendAsync<LoginResponseDto>(HttpMethod.Post, "api/auth/login", new
        {
            Email = email,
            Password = password,
            RememberMe = rememberMe
        });

    public Task<ApiResponse<UserPagedResult>> GetUsersAsync(
        string? roleFilter, string? search, bool? isActive, int page, int pageSize, int? facilityId = null)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };
        if (!string.IsNullOrWhiteSpace(roleFilter)) query.Add($"roleFilter={Uri.EscapeDataString(roleFilter)}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (isActive.HasValue) query.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        if (facilityId.HasValue) query.Add($"facilityId={facilityId.Value}");
        return SendAsync<UserPagedResult>(HttpMethod.Get, $"api/users?{string.Join("&", query)}");
    }

    public Task<ApiResponse<List<FacilityOptionViewModel>>> GetFacilityOptionsAsync() =>
        SendAsync<List<FacilityOptionViewModel>>(HttpMethod.Get, "api/users/facility-options");

    public Task<ApiResponse<List<RoleOptionDto>>> GetRolesAsync() =>
        SendAsync<List<RoleOptionDto>>(HttpMethod.Get, "api/users/roles");

    public Task<ApiResponse<FacilitiesDataDto>> GetFacilitiesAsync() =>
        SendAsync<FacilitiesDataDto>(HttpMethod.Get, "api/users/facilities");

    public Task<ApiResponse<UserDto>> GetUserAsync(int id) =>
        SendAsync<UserDto>(HttpMethod.Get, $"api/users/{id}");

    public Task<ApiResponse<UserDto>> CreateUserAsync(UserCreateDto request) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/users", request);

    public Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UserUpdateDto request) =>
        SendAsync<UserDto>(HttpMethod.Put, $"api/users/{id}", request);

    public Task<ApiResponse> SetUserActiveAsync(int id, bool isActive) =>
        SendAsync(HttpMethod.Put, $"api/users/{id}/active", new { IsActive = isActive });

    public Task<ApiResponse> DeleteUserAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/users/{id}");

    public Task<ApiResponse> ResetPasswordAsync(int id, string newPassword) =>
        SendAsync(HttpMethod.Post, $"api/users/{id}/reset-password", new { NewPassword = newPassword });

    public Task<ApiResponse> AssignFacilityManagerAsync(int facilityId, int? managerId) =>
        SendAsync(HttpMethod.Put, $"api/users/facilities/{facilityId}/manager", new { ManagerId = managerId });

    public Task<ApiResponse<AdminDashboardViewModel>> GetAdminDashboardAsync(DateOnly date) =>
        SendAsync<AdminDashboardViewModel>(HttpMethod.Get, $"api/admin/dashboard?date={date:yyyy-MM-dd}");

    public Task<ApiResponse<AdminDashboardViewModel>> GetFacilityManagerDashboardAsync(DateOnly date) =>
        SendAsync<AdminDashboardViewModel>(HttpMethod.Get, $"api/facility-manager/dashboard?date={date:yyyy-MM-dd}");

    private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request);
            ApiResponse<T>? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            }
            catch (JsonException)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = BuildNonJsonResponseMessage(response)
                };
            }

            if (result != null && (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(result.Message)))
            {
                result.Success = false;
                result.Message = $"API trả về trạng thái {(int)response.StatusCode} ({response.ReasonPhrase}).";
            }
            return result ?? new ApiResponse<T>
            {
                Success = false,
                Message = $"API trả về trạng thái {(int)response.StatusCode} ({response.ReasonPhrase})."
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<T> { Success = false, Message = "Không kết nối được tới BackEnd API." };
        }
    }

    private async Task<ApiResponse> SendAsync(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request);
            ApiResponse? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<ApiResponse>(JsonOptions);
            }
            catch (JsonException)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = BuildNonJsonResponseMessage(response)
                };
            }

            if (result != null && (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(result.Message)))
            {
                result.Success = false;
                result.Message = $"API trả về trạng thái {(int)response.StatusCode} ({response.ReasonPhrase}).";
            }
            return result ?? new ApiResponse
            {
                Success = false,
                Message = $"API trả về trạng thái {(int)response.StatusCode} ({response.ReasonPhrase})."
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse { Success = false, Message = "Không kết nối được tới BackEnd API." };
        }
    }

    private static string BuildNonJsonResponseMessage(HttpResponseMessage response)
    {
        if ((int)response.StatusCode == 401)
        {
            return "BackEnd API từ chối xác thực (HTTP 401). Hãy đăng xuất rồi đăng nhập lại; đồng thời kiểm tra BackEnd đang chạy ở HTTPS localhost:7002.";
        }

        return $"BackEnd API trả về dữ liệu không phải JSON hợp lệ (HTTP {(int)response.StatusCode}, Content-Type {response.Content.Headers.ContentType?.MediaType ?? "không có"}).";
    }
}

public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
