using FrontEnd.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace FrontEnd.Services.Api;

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

    public Task<ApiResponse<List<ClassListItemViewModel>>> GetClassesAsync() =>
        SendAsync<List<ClassListItemViewModel>>(HttpMethod.Get, "api/classes");

    public Task<ApiResponse<ClassOptionsViewModel>> GetClassOptionsAsync() =>
        SendAsync<ClassOptionsViewModel>(HttpMethod.Get, "api/classes/options");

    public Task<ApiResponse<ClassDetailViewModel>> GetClassAsync(int classId) =>
        SendAsync<ClassDetailViewModel>(HttpMethod.Get, $"api/classes/{classId}");

    public Task<ApiResponse<List<ScheduleConflictViewModel>>> PreviewClassConflictsAsync(ClassWriteViewModel request, int? excludeClassId = null) =>
        SendAsync<List<ScheduleConflictViewModel>>(HttpMethod.Post,
            excludeClassId.HasValue ? $"api/classes/preview-conflicts?excludeClassId={excludeClassId.Value}" : "api/classes/preview-conflicts",
            request);

    public Task<ApiResponse<ClassDetailViewModel>> CreateClassAsync(ClassWriteViewModel request) =>
        SendAsync<ClassDetailViewModel>(HttpMethod.Post, "api/classes", request);

    public Task<ApiResponse<ClassDetailViewModel>> UpdateClassAsync(int classId, ClassWriteViewModel request) =>
        SendAsync<ClassDetailViewModel>(HttpMethod.Put, $"api/classes/{classId}", request);

    public Task<ApiResponse> ChangeClassLecturerAsync(int classId, int lecturerId) =>
        SendAsync(HttpMethod.Put, $"api/classes/{classId}/lecturer", new { LecturerId = lecturerId });

    public Task<ApiResponse> CloseClassAsync(int classId) =>
        SendAsync(HttpMethod.Post, $"api/classes/{classId}/close");

    public Task<ApiResponse> GenerateClassSessionsAsync(int classId) =>
        SendAsync(HttpMethod.Post, $"api/classes/{classId}/generate-sessions");

    public Task<ApiResponse> RescheduleSessionAsync(int sessionId, SessionRescheduleViewModel request) =>
        SendAsync(HttpMethod.Put, $"api/sessions/{sessionId}/reschedule", request);

    public Task<ApiResponse> CancelSessionAsync(int sessionId) =>
        SendAsync(HttpMethod.Post, $"api/sessions/{sessionId}/cancel");

    public Task<ApiResponse> AssignSessionSubstituteAsync(int sessionId, int lecturerId) =>
        SendAsync(HttpMethod.Put, $"api/sessions/{sessionId}/substitute", new { LecturerId = lecturerId });

    public Task<ApiResponse<EnrollmentOptionsViewModel>> GetEnrollmentOptionsAsync() =>
        SendAsync<EnrollmentOptionsViewModel>(HttpMethod.Get, "api/enrollments/options");

    public Task<ApiResponse<List<EnrollmentViewModel>>> GetEnrollmentsAsync(string? search, int? classId, string? status) =>
        SendAsync<List<EnrollmentViewModel>>(HttpMethod.Get,
            $"api/enrollments?search={Uri.EscapeDataString(search ?? string.Empty)}&classId={classId}&status={Uri.EscapeDataString(status ?? "All")}");

    public Task<ApiResponse<EnrollmentPreviewViewModel>> PreviewEnrollmentAsync(EnrollmentRequestViewModel request) =>
        SendAsync<EnrollmentPreviewViewModel>(HttpMethod.Post, "api/enrollments/preview", request);

    public Task<ApiResponse<EnrollmentViewModel>> EnrollStudentAsync(EnrollmentRequestViewModel request) =>
        SendAsync<EnrollmentViewModel>(HttpMethod.Post, "api/enrollments", request);

    public Task<ApiResponse> DropEnrollmentAsync(int enrollmentId) =>
        SendAsync(HttpMethod.Put, $"api/enrollments/{enrollmentId}/drop");

    public Task<ApiResponse<List<TuitionPreviewItemViewModel>>> GetTuitionPreviewAsync(DateOnly from, DateOnly to, int? classId = null) =>
        SendAsync<List<TuitionPreviewItemViewModel>>(HttpMethod.Get,
            $"api/tuitions/preview?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&classId={classId}");

    public Task<ApiResponse<List<TuitionRecordViewModel>>> GetTuitionsAsync(DateOnly from, DateOnly to, int? classId = null) =>
        SendAsync<List<TuitionRecordViewModel>>(HttpMethod.Get,
            $"api/tuitions?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&classId={classId}");

    public Task<ApiResponse<TuitionIssueResultViewModel>> IssueTuitionsAsync(TuitionIssueRequestViewModel request) =>
        SendAsync<TuitionIssueResultViewModel>(HttpMethod.Post, "api/tuitions/issue", request);

    public Task<ApiResponse<TuitionRecordViewModel>> RecordTuitionPaymentAsync(RecordPaymentViewModel request) =>
        SendAsync<TuitionRecordViewModel>(HttpMethod.Post, $"api/tuitions/{request.TuitionId}/payments", new
        {
            request.AmountPaid,
            request.PaymentMethod,
            request.ReceiptNumber
        });

    public Task<ApiResponse<List<StudentPortalScheduleItemViewModel>>> GetMyStudentScheduleAsync(DateOnly from, DateOnly to) =>
        SendAsync<List<StudentPortalScheduleItemViewModel>>(HttpMethod.Get,
            $"api/student-portal/schedule?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    public Task<ApiResponse<List<StudentPortalScheduleItemViewModel>>> GetMyStudentAttendanceAsync(DateOnly from, DateOnly to) =>
        SendAsync<List<StudentPortalScheduleItemViewModel>>(HttpMethod.Get,
            $"api/student-portal/attendance?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    public Task<ApiResponse<List<TuitionRecordViewModel>>> GetMyTuitionsAsync() =>
        SendAsync<List<TuitionRecordViewModel>>(HttpMethod.Get, "api/student-portal/tuitions");

    public Task<ApiResponse<List<RoomShiftStatusViewModel>>> GetRoomStatusGridAsync(DateOnly date, int? facilityId = null)
    {
        var path = $"api/rooms/status?date={date:yyyy-MM-dd}";
        if (facilityId.HasValue) path += $"&facilityId={facilityId.Value}";
        return SendAsync<List<RoomShiftStatusViewModel>>(HttpMethod.Get, path);
    }

    public Task<ApiResponse<List<RoomOptionViewModel>>> FindEmptyRoomsAsync(DateOnly date, int shiftId) =>
        SendAsync<List<RoomOptionViewModel>>(HttpMethod.Get, $"api/rooms/empty?date={date:yyyy-MM-dd}&shiftId={shiftId}");

    public Task<ApiResponse<List<AffectedSessionViewModel>>> GetAffectedRoomSessionsAsync(int roomId) =>
        SendAsync<List<AffectedSessionViewModel>>(HttpMethod.Get, $"api/rooms/{roomId}/affected-sessions");

    public Task<ApiResponse> SetRoomConditionAsync(int roomId, string condition) =>
        SendAsync(HttpMethod.Put, $"api/rooms/{roomId}/condition", new { Condition = condition });

    public Task<ApiResponse> AssignRoomToSessionAsync(int sessionId, int roomId, bool applyToWholeClass) =>
        SendAsync(HttpMethod.Put, $"api/rooms/sessions/{sessionId}/room", new { RoomId = roomId, ApplyToWholeClass = applyToWholeClass });

    public Task<ApiResponse> CancelAffectedRoomSessionAsync(int sessionId) =>
        SendAsync(HttpMethod.Post, $"api/rooms/sessions/{sessionId}/cancel");

    public Task<ApiResponse<List<RoomChangeReviewViewModel>>> GetPendingRoomChangeRequestsAsync() =>
        SendAsync<List<RoomChangeReviewViewModel>>(HttpMethod.Get, "api/room-change-requests");

    public Task<ApiResponse> ApproveRoomChangeRequestAsync(int requestId, string? reviewNote) =>
        SendAsync(HttpMethod.Post, $"api/room-change-requests/{requestId}/approve", new { ReviewNote = reviewNote });

    public Task<ApiResponse> RejectRoomChangeRequestAsync(int requestId, string reviewNote) =>
        SendAsync(HttpMethod.Post, $"api/room-change-requests/{requestId}/reject", new { ReviewNote = reviewNote });

    public Task<ApiResponse<List<LecturerScheduleSessionViewModel>>> GetLecturerScheduleAsync(DateOnly from, DateOnly to) =>
        SendAsync<List<LecturerScheduleSessionViewModel>>(HttpMethod.Get,
            $"api/lecturer/schedule?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    public Task<ApiResponse<AttendanceRosterViewModel>> GetAttendanceRosterAsync(int sessionId) =>
        SendAsync<AttendanceRosterViewModel>(HttpMethod.Get, $"api/attendance/{sessionId}/roster");

    public Task<ApiResponse> SaveAttendanceAsync(int sessionId, IEnumerable<AttendanceRosterStudentViewModel> students) =>
        SendAsync(HttpMethod.Put, $"api/attendance/{sessionId}", new
        {
            Items = students.Select(student => new
            {
                student.StudentId,
                Status = student.AttendanceStatus,
                student.Note
            }).ToList()
        });

    public Task<ApiResponse> CompleteAttendanceSessionAsync(int sessionId) =>
        SendAsync(HttpMethod.Post, $"api/attendance/{sessionId}/complete");

    public Task<ApiResponse<LecturerChangeRequestOptionsViewModel>> GetLecturerChangeRequestOptionsAsync() =>
        SendAsync<LecturerChangeRequestOptionsViewModel>(HttpMethod.Get, "api/room-change-requests/options");

    public Task<ApiResponse<LecturerRoomChangeRequestViewModel>> CreateLecturerRoomChangeRequestAsync(
        int classId, int? sessionId, int? requestedRoomId, int? requestedShiftId, string reason) =>
        SendAsync<LecturerRoomChangeRequestViewModel>(HttpMethod.Post, "api/room-change-requests", new
        {
            ClassId = classId,
            SessionId = sessionId,
            RequestedRoomId = requestedRoomId,
            RequestedShiftId = requestedShiftId,
            Reason = reason
        });

    public Task<ApiResponse<List<LecturerRoomChangeRequestViewModel>>> GetMyRoomChangeRequestsAsync() =>
        SendAsync<List<LecturerRoomChangeRequestViewModel>>(HttpMethod.Get, "api/room-change-requests/mine");

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
