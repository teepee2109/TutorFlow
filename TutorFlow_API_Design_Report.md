# TutorFlow API Design

## 2. Detailed API Design

This section documents the ASP.NET Core Web API used by TutorFlow. The FrontEnd MVC project sends HTTP requests to the BackEnd API, which processes the request through its controllers and services and reads or updates SQL Server through Entity Framework Core. The endpoint definitions below follow the current API routes and DTOs in the solution.

### 2.0 API conventions

- **Development base URL:** `https://localhost:7002`. The HTTP profile is `http://localhost:5228`.
- **API documentation:** `https://localhost:7002/swagger` while the BackEnd runs in the Development environment.
- **Content type:** JSON requests use `Content-Type: application/json`; successful and handled error responses are JSON.
- **Authentication:** except for login, protected endpoints require `Authorization: Bearer <accessToken>`. The token is issued by `POST /api/auth/login`.
- **JSON naming:** DTO property names are serialized in camelCase. `DateOnly` values use `yyyy-MM-dd`; time values use `HH:mm:ss`.
- **Generic success response:** `ApiResponse<T>` contains `success`, `value`, and `message`. Operations without a returned object use `ApiResponse`, containing `success` and `message`.
- **Typical error codes:** `400` validation or business-rule error; `401` missing or invalid authentication; `403` authenticated user lacks the required role; `404` resource not found; `409` schedule or database conflict; `500` unexpected server error. Automatic model validation may return ASP.NET Core validation details.

## 2.1 Authentication API

### 2.1.1 Login

- **URL / method:** `POST /api/auth/login`
- **Authorization:** Anonymous; no JWT is required.
- **Parameters:** None.
- **Request body:** `LoginRequest` — `email` (string), `password` (string), `rememberMe` (boolean).
- **Return type:** `ApiResponse<LoginResponse>`. `value` contains `accessToken` (string), `expiresAtUtc` (DateTime), and `user` (`userId`, `fullName`, `email`, `roleName`, nullable `facilityId`, nullable `facilityName`).
- **Responses:** `200 OK` for valid credentials; `401 Unauthorized` for invalid credentials.

Example request:

```json
{
  "email": "gm@tutorflow.local",
  "password": "<password>",
  "rememberMe": false
}
```

## 2.2 Dashboard APIs

### 2.2.1 Get system dashboard

- **URL / method:** `GET /api/admin/dashboard`
- **Authorization:** `Admin`.
- **Parameters:** Optional query parameter `date` (`yyyy-MM-dd`); defaults to today.
- **Request body:** None.
- **Return type:** `ApiResponse<AdminDashboardDto>` with target date, system room/class/user/request counts, unpaid tuition total, and `roomShiftStatuses`.
- **Responses:** `200 OK`, `404 Not Found`, or `500 Internal Server Error`.

### 2.2.2 Get facility dashboard

- **URL / method:** `GET /api/facility-manager/dashboard`
- **Authorization:** `FacilityManager`.
- **Parameters:** Optional query parameter `date` (`yyyy-MM-dd`); defaults to today. The facility is taken from the authenticated Facility Manager account.
- **Request body:** None.
- **Return type:** `ApiResponse<AdminDashboardDto>` with facility-level counts and room/shift status.
- **Responses:** `200 OK`; `403 Forbidden` if the account has no assigned facility; `404 Not Found`; `500 Internal Server Error`.

## 2.3 User and facility APIs

### 2.3.1 List users

- **URL / method:** `GET /api/users`
- **Authorization:** `Admin`, `GrandManager`.
- **Parameters:** Optional query parameters `roleFilter` (string), `search` (string), `isActive` (boolean), `page` (integer, default `1`), `pageSize` (integer, default `10`), and `facilityId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse<UserPagedResult>`. The value contains `items` (UserDto array), `totalCount`, `page`, `pageSize`, and `totalPages`. Each user contains `userId`, `fullName`, `email`, nullable `phoneNumber`, `roleId`, `roleName`, `isActive`, `createdAt`, nullable `assignedFacilityId`, and nullable `assignedFacilityName`.
- **Responses:** `200 OK`; `400 Bad Request` if the query cannot be processed.

### 2.3.2 Get facility options

- **URL / method:** `GET /api/users/facility-options`
- **Authorization:** `Admin`, `GrandManager`.
- **Parameters / request body:** None.
- **Return type:** `ApiResponse<List<FacilityOptionDto>>`; each option contains `facilityId` and `facilityName`.
- **Responses:** `200 OK`.

### 2.3.3 Get role options

- **URL / method:** `GET /api/users/roles`
- **Authorization:** `Admin`, `GrandManager`.
- **Parameters / request body:** None.
- **Return type:** `ApiResponse<List<RoleOptionDto>>`; each option contains `roleId` and `roleName`. A Grand Manager does not receive the Admin or GrandManager role options.
- **Responses:** `200 OK`.

### 2.3.4 List facilities and eligible managers

- **URL / method:** `GET /api/users/facilities`
- **Authorization:** `Admin` only.
- **Parameters / request body:** None.
- **Return type:** `ApiResponse<FacilitiesDataDto>`. The value contains `facilities` (facility ID, name, nullable address and manager details, room count) and `eligibleManagers` (UserDto array).
- **Responses:** `200 OK`; `403 Forbidden` for other roles.

### 2.3.5 Assign or unassign a facility manager

- **URL / method:** `PUT /api/users/facilities/{facilityId}/manager`
- **Authorization:** `Admin` only.
- **Path parameter:** `facilityId` (integer).
- **Request body:** `AssignFacilityManagerRequest` — `managerId` (nullable integer). `null` removes the current assignment.
- **Return type:** `ApiResponse` with a result message.
- **Responses:** `200 OK`; `400 Bad Request` for an invalid assignment; `403 Forbidden` for other roles.

### 2.3.6 Get user details

- **URL / method:** `GET /api/users/{id}`
- **Authorization:** `Admin`, `GrandManager`.
- **Path parameter:** `id` (integer user ID).
- **Request body:** None.
- **Return type:** `ApiResponse<UserDto>`.
- **Responses:** `200 OK`; `404 Not Found`.

### 2.3.7 Create a user

- **URL / method:** `POST /api/users`
- **Authorization:** `Admin`, `GrandManager`.
- **Parameters:** None.
- **Request body:** `UserCreateDto` — `fullName` (string), `email` (string), nullable `phoneNumber` (string), `roleId` (integer), `password` (string).
- **Return type:** `ApiResponse<UserDto>`; the response does not return the password or password hash.
- **Responses:** `200 OK`; `400 Bad Request` for invalid input or a business-rule violation.

### 2.3.8 Update a user

- **URL / method:** `PUT /api/users/{id}`
- **Authorization:** `Admin`, `GrandManager`.
- **Path parameter:** `id` (integer user ID; it is assigned to the DTO by the API).
- **Request body:** `UserUpdateDto` — `fullName`, nullable `phoneNumber`, and `roleId`. The body `userId`, if supplied, is overwritten by the path value.
- **Return type:** `ApiResponse<UserDto>`.
- **Responses:** `200 OK`; `400 Bad Request` for invalid updates.

### 2.3.9 Change active status

- **URL / method:** `PUT /api/users/{id}/active`
- **Authorization:** `Admin`, `GrandManager`.
- **Path parameter:** `id` (integer user ID).
- **Request body:** `UserActiveRequest` — `isActive` (boolean).
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request` if the update is not allowed.

### 2.3.10 Soft-delete a user

- **URL / method:** `DELETE /api/users/{id}`
- **Authorization:** `Admin`, `GrandManager`.
- **Path parameter:** `id` (integer user ID).
- **Request body:** None.
- **Return type:** `ApiResponse`. The API performs a soft delete rather than physically removing the database row.
- **Responses:** `200 OK`; `400 Bad Request` if the operation is not allowed.

### 2.3.11 Reset a user's password

- **URL / method:** `POST /api/users/{id}/reset-password`
- **Authorization:** `Admin`, `GrandManager`.
- **Path parameter:** `id` (integer user ID).
- **Request body:** `ResetPasswordRequest` — `newPassword` (string).
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request` for invalid input or a business-rule violation.

## 2.4 Class and session APIs

All APIs in this section require the `GrandManager` role.

### 2.4.1 List classes

- **URL / method:** `GET /api/classes`
- **Parameters / request body:** None.
- **Return type:** `ApiResponse<List<ClassListItemDto>>`. Each item includes class, subject, lecturer, facility, room, shift, days, date range, status, and session count.
- **Responses:** `200 OK`; `400 Bad Request` for a service error.

### 2.4.2 Get class form options

- **URL / method:** `GET /api/classes/options`
- **Parameters / request body:** None.
- **Return type:** `ApiResponse<ClassOptionsDto>` with lists of subjects, lecturers, facilities, rooms, and shifts. Each lookup item contains `id`, `name`, and optional facility/capacity/condition values.
- **Responses:** `200 OK`; `400 Bad Request` for a service error.

### 2.4.3 Get class details

- **URL / method:** `GET /api/classes/{classId}`
- **Path parameter:** `classId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse<ClassDetailDto>`; class details include a `sessions` array with session date, room, shift, lecturer, and status.
- **Responses:** `200 OK`; `404 Not Found`.

### 2.4.4 Preview class schedule conflicts

- **URL / method:** `POST /api/classes/preview-conflicts`
- **Query parameter:** Optional `excludeClassId` (integer), used when editing an existing class.
- **Request body:** `ClassWriteDto` — `className`, `subjectId`, `lecturerId`, `facilityId`, `roomId`, `shiftId`, `daysOfWeek`, `startDate`, and `endDate`.
- **Return type:** `ApiResponse<List<ScheduleConflictDto>>`. Each item includes `date`, `error`, `conflictType`, nullable `conflictingClassName`, and `message`.
- **Responses:** `200 OK` with an empty list when there are no conflicts; `400 Bad Request` for validation errors.

### 2.4.5 Create a class

- **URL / method:** `POST /api/classes`
- **Request body:** `ClassWriteDto` — class name, subject, lecturer, facility, room, shift, days of week, start date, and end date.
- **Return type:** `ApiResponse<ClassDetailDto>`; includes the created class and generated sessions.
- **Responses:** `200 OK`; `401 Unauthorized` if the current user ID is unavailable; `400 Bad Request`; `404 Not Found`; `409 Conflict` for schedule or room conflicts.

### 2.4.6 Update a class

- **URL / method:** `PUT /api/classes/{classId}`
- **Path parameter:** `classId` (integer).
- **Request body:** `ClassWriteDto`.
- **Return type:** `ApiResponse<ClassDetailDto>` with updated class and upcoming sessions.
- **Responses:** `200 OK`; `400 Bad Request`; `404 Not Found`; `409 Conflict`.

### 2.4.7 Change a class lecturer

- **URL / method:** `PUT /api/classes/{classId}/lecturer`
- **Path parameter:** `classId` (integer).
- **Request body:** `ChangeLecturerDto` — `lecturerId` (integer).
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `404 Not Found`; `409 Conflict` when the new assignment conflicts with the schedule.

### 2.4.8 Close a class

- **URL / method:** `POST /api/classes/{classId}/close`
- **Path parameter:** `classId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `404 Not Found`.

### 2.4.9 Generate class sessions

- **URL / method:** `POST /api/classes/{classId}/generate-sessions`
- **Path parameter:** `classId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `404 Not Found`; `409 Conflict` for duplicate or conflicting sessions.

### 2.4.10 Reschedule a session

- **URL / method:** `PUT /api/sessions/{sessionId}/reschedule`
- **Path parameter:** `sessionId` (integer).
- **Request body:** `SessionRescheduleDto` — nullable `roomId`, nullable `shiftId`, nullable `sessionDate`, and `applyToWholeClass` (boolean).
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `404 Not Found`; `409 Conflict` for room, lecturer, student, or duplicate-session conflicts.

### 2.4.11 Cancel a session

- **URL / method:** `POST /api/sessions/{sessionId}/cancel`
- **Path parameter:** `sessionId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `404 Not Found`; `409 Conflict` if cancellation violates a schedule rule.

### 2.4.12 Assign a substitute lecturer

- **URL / method:** `PUT /api/sessions/{sessionId}/substitute`
- **Path parameter:** `sessionId` (integer).
- **Request body:** `AssignSubstituteDto` — `lecturerId` (integer).
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `404 Not Found`; `409 Conflict` if the substitute has a schedule conflict.

## 2.5 Room and room-change APIs

### 2.5.1 Get room and shift status

- **URL / method:** `GET /api/rooms/status`
- **Authorization:** `Admin`, `GrandManager`, `FacilityManager`, `Lecturer`.
- **Parameters:** Optional query parameters `date` (`yyyy-MM-dd`, defaults to today) and `facilityId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse<List<RoomShiftStatusDto>>`; each row contains facility, room, condition, shift, time, slot status, and optional class/session/lecturer details.
- **Responses:** `200 OK`; `400 Bad Request` for a service error.

### 2.5.2 Find empty rooms

- **URL / method:** `GET /api/rooms/empty`
- **Authorization:** `FacilityManager`.
- **Parameters:** Required query parameters `date` (`yyyy-MM-dd`) and `shiftId` (integer). The facility is derived from the signed-in Facility Manager.
- **Request body:** None.
- **Return type:** `ApiResponse<List<RoomOptionDto>>`.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden` for other roles.

### 2.5.3 Get sessions affected by a room

- **URL / method:** `GET /api/rooms/{roomId}/affected-sessions`
- **Authorization:** `FacilityManager`.
- **Path parameter:** `roomId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse<List<AffectedSessionDto>>`; each session includes class/date/shift/current room/lecturer and available rooms.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`.

### 2.5.4 Change room maintenance condition

- **URL / method:** `PUT /api/rooms/{roomId}/condition`
- **Authorization:** `FacilityManager` for the manager's own facility.
- **Path parameter:** `roomId` (integer).
- **Request body:** `RoomConditionRequest` — `condition` must be `Open` or `Closed`.
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`; `409 Conflict` if an upcoming session prevents closing the room.

### 2.5.5 Assign a room to a session

- **URL / method:** `PUT /api/rooms/sessions/{sessionId}/room`
- **Authorization:** `FacilityManager` for the manager's own facility.
- **Path parameter:** `sessionId` (integer).
- **Request body:** `RoomAssignmentRequest` — `roomId` (integer) and `applyToWholeClass` (boolean).
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`; `409 Conflict` for a room or schedule conflict.

### 2.5.6 Cancel a session affected by room maintenance

- **URL / method:** `POST /api/rooms/sessions/{sessionId}/cancel`
- **Authorization:** `FacilityManager` for the manager's own facility.
- **Path parameter:** `sessionId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`.

### 2.5.7 List pending room-change requests

- **URL / method:** `GET /api/room-change-requests`
- **Authorization:** `FacilityManager`; results are scoped by the manager's facility.
- **Parameters / request body:** None.
- **Return type:** `ApiResponse<List<RoomChangeReviewDto>>`; each item includes request/class/lecturer/scope/date/current and requested room/shift/reason/created date.
- **Responses:** `200 OK`; `401 Unauthorized`; `403 Forbidden`.

### 2.5.8 Approve a room-change request

- **URL / method:** `POST /api/room-change-requests/{requestId}/approve`
- **Authorization:** `FacilityManager` for the request's facility.
- **Path parameter:** `requestId` (integer).
- **Request body:** `ReviewNoteRequest` — nullable `reviewNote` (up to 255 characters).
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`; `409 Conflict` if applying the request creates a schedule conflict.

### 2.5.9 Reject a room-change request

- **URL / method:** `POST /api/room-change-requests/{requestId}/reject`
- **Authorization:** `FacilityManager` for the request's facility.
- **Path parameter:** `requestId` (integer).
- **Request body:** `RejectRoomChangeRequest` — required `reviewNote` (1–255 characters).
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`.

### 2.5.10 Get lecturer room-change request options

- **URL / method:** `GET /api/room-change-requests/options`
- **Authorization:** `Lecturer`.
- **Parameters / request body:** None. The API derives the lecturer from the JWT.
- **Return type:** `ApiResponse<LecturerChangeRequestOptionsDto>` containing eligible scopes, rooms, and shifts.
- **Responses:** `200 OK`; `401 Unauthorized`; `403 Forbidden`; `400 Bad Request` for a service error.

### 2.5.11 List the current lecturer's requests

- **URL / method:** `GET /api/room-change-requests/mine`
- **Authorization:** `Lecturer`.
- **Parameters / request body:** None. The API derives the lecturer from the JWT.
- **Return type:** `ApiResponse<List<LecturerRoomChangeRequestDto>>`; each item includes class/scope/session, current and requested room/shift, reason, status, and review information.
- **Responses:** `200 OK`; `401 Unauthorized`; `403 Forbidden`; `400 Bad Request` for a service error.

### 2.5.12 Create a room or shift change request

- **URL / method:** `POST /api/room-change-requests`
- **Authorization:** `Lecturer`.
- **Request body:** `LecturerChangeRequestCreateDto` — `classId` (integer), nullable `sessionId`, nullable `requestedRoomId`, nullable `requestedShiftId`, and required `reason` (up to 500 characters). At least one requested room or shift must be supplied by the business flow.
- **Return type:** `ApiResponse<LecturerRoomChangeRequestDto>`.
- **Responses:** `200 OK`; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict` for schedule conflicts.

## 2.6 Lecturer schedule and attendance APIs

### 2.6.1 Get the lecturer's schedule

- **URL / method:** `GET /api/lecturer/schedule`
- **Authorization:** `Lecturer`.
- **Parameters:** Optional query parameters `from` and `to` (`yyyy-MM-dd`). Defaults to the current Monday through Sunday; when only `from` is provided, `to` defaults to six days later.
- **Request body:** None.
- **Return type:** `ApiResponse<List<LecturerScheduleSessionDto>>`; each item contains session/class/date/shift/time/room/facility/status.
- **Responses:** `200 OK`; `401 Unauthorized`; `403 Forbidden`; `400 Bad Request` for an invalid range.

### 2.6.2 Get a session attendance roster

- **URL / method:** `GET /api/attendance/{sessionId}/roster`
- **Authorization:** `Lecturer` assigned to the session.
- **Path parameter:** `sessionId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse<AttendanceRosterDto>`; includes session details and students with nullable current `attendanceStatus` and `note`.
- **Responses:** `200 OK`; `403 Forbidden` if the lecturer is not assigned to the session; `404 Not Found`.

### 2.6.3 Save attendance

- **URL / method:** `PUT /api/attendance/{sessionId}`
- **Authorization:** `Lecturer` assigned to the session.
- **Path parameter:** `sessionId` (integer).
- **Request body:** `AttendanceSaveDto` — `items` array. Each item contains `studentId` (integer), `status` (`Present`, `ExcusedAbsent`, or `UnexcusedAbsent`), and optional `note` (up to 255 characters).
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict` if the student is not enrolled or the session is cancelled/future.

### 2.6.4 Complete an attendance session

- **URL / method:** `POST /api/attendance/{sessionId}/complete`
- **Authorization:** `Lecturer` assigned to the session.
- **Path parameter:** `sessionId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict` if the session cannot be completed.

## 2.7 Enrollment APIs

All enrollment management APIs require the `GrandManager` role. The Admin role does not have enrollment permission in the project role matrix.

### 2.7.1 Get enrollment form options

- **URL / method:** `GET /api/enrollments/options`
- **Parameters / request body:** None.
- **Return type:** `ApiResponse<EnrollmentOptionsDto>` with active students and active classes.
- **Responses:** `200 OK`; `401 Unauthorized`; `403 Forbidden`.

### 2.7.2 List enrollments

- **URL / method:** `GET /api/enrollments`
- **Parameters:** Optional query parameters `search` (student name/email or class name), `classId` (integer), and `status` (`Active` or `Dropped`).
- **Request body:** None.
- **Return type:** `ApiResponse<List<EnrollmentDto>>`; each item includes enrollment/student/class IDs and names, `enrollDate`, and status.
- **Responses:** `200 OK`; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`.

### 2.7.3 Preview enrollment schedule conflicts

- **URL / method:** `POST /api/enrollments/preview`
- **Request body:** `EnrollmentRequestDto` — `studentId` (integer), `classId` (integer), `enrollDate` (`yyyy-MM-dd`; defaults to today).
- **Return type:** `ApiResponse<EnrollmentPreviewDto>` with `alreadyEnrolled`, `conflicts` (date, shift, conflicting class), and computed `canEnroll`.
- **Responses:** `200 OK` with conflict details; `400 Bad Request`; `403 Forbidden` when the selected account is not an active student; `404 Not Found` for an unavailable class. This preview returns conflicts as data; the enrollment operation returns `409 Conflict` when a conflict blocks enrollment.

### 2.7.4 Enroll or reactivate a student

- **URL / method:** `POST /api/enrollments`
- **Request body:** `EnrollmentRequestDto` — `studentId`, `classId`, and `enrollDate`.
- **Return type:** `ApiResponse<EnrollmentDto>`.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden` for a role error; `404 Not Found`; `409 Conflict` when the student has overlapping class sessions. An existing dropped enrollment is reactivated by this operation.

### 2.7.5 Drop an enrollment

- **URL / method:** `PUT /api/enrollments/{enrollmentId}/drop`
- **Path parameter:** `enrollmentId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse`.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`.

## 2.8 Tuition and payment APIs

Read endpoints allow `Admin`, `GrandManager`, and `Lecturer`. Tuition issuance and payment recording require `GrandManager`.

### 2.8.1 Preview tuition from attendance

- **URL / method:** `GET /api/tuitions/preview`
- **Parameters:** Required query parameters `from` and `to` (`yyyy-MM-dd`); optional `classId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse<List<TuitionPreviewItemDto>>`; each item contains student/class, present/excused/unexcused counts, total sessions, amount per session, calculated tuition amount, and `alreadyIssued`.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`.

### 2.8.2 List issued tuition records

- **URL / method:** `GET /api/tuitions`
- **Parameters:** Required query parameters `from` and `to` (`yyyy-MM-dd`); optional `classId` (integer).
- **Request body:** None.
- **Return type:** `ApiResponse<List<TuitionRecordDto>>`; each record contains student/class, period, sessions attended, amount per session, total/paid/remaining amounts, payment status, due date, and payment history.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`.

### 2.8.3 Issue tuition records

- **URL / method:** `POST /api/tuitions/issue`
- **Authorization:** `GrandManager`.
- **Request body:** `TuitionIssueRequestDto` — `periodFrom` (`yyyy-MM-dd`), `periodTo` (`yyyy-MM-dd`), optional `classId` (integer).
- **Return type:** `ApiResponse<TuitionIssueResultDto>` with `issuedCount`, `skippedExistingCount`, and `skippedNoAttendanceCount`.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`.

### 2.8.4 Record a tuition payment

- **URL / method:** `POST /api/tuitions/{tuitionId}/payments`
- **Authorization:** `GrandManager`.
- **Path parameter:** `tuitionId` (integer).
- **Request body:** `PaymentCreateDto` — `amountPaid` (decimal, at least 1), optional `paymentMethod` (up to 50 characters), optional `receiptNumber` (up to 50 characters).
- **Return type:** `ApiResponse<TuitionRecordDto>` with updated balance/status and payment history.
- **Responses:** `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`.

## 2.9 Student portal APIs

All student portal APIs require the `Student` role and derive the student ID from the authenticated JWT; the client cannot request another student's data by supplying a user ID.

### 2.9.1 Get the student's schedule

- **URL / method:** `GET /api/student-portal/schedule`
- **Parameters:** Optional query parameters `from` and `to` (`yyyy-MM-dd`). The default range starts 30 days before today and ends 60 days after the selected start date.
- **Request body:** None.
- **Return type:** `ApiResponse<List<StudentPortalScheduleItemDto>>`; items include class/session/date/shift/time/room/facility/lecturer/status and nullable attendance status/note.
- **Responses:** `200 OK`; `401 Unauthorized`; `403 Forbidden`; `400 Bad Request` for invalid input.

### 2.9.2 Get the student's attendance history

- **URL / method:** `GET /api/student-portal/attendance`
- **Parameters:** Optional query parameters `from` and `to` (`yyyy-MM-dd`), using the same default range as the schedule endpoint.
- **Request body:** None.
- **Return type:** `ApiResponse<List<StudentPortalScheduleItemDto>>`, filtered to sessions with attendance or completed sessions.
- **Responses:** `200 OK`; `401 Unauthorized`; `403 Forbidden`; `400 Bad Request` for invalid input.

### 2.9.3 Get the student's tuition records

- **URL / method:** `GET /api/student-portal/tuitions`
- **Parameters / request body:** None. The student ID is taken from the JWT.
- **Return type:** `ApiResponse<List<TuitionRecordDto>>` with the student's issued tuition records and payment history.
- **Responses:** `200 OK`; `401 Unauthorized`; `403 Forbidden`; `400 Bad Request` for a service error.

## 2.10 FrontEnd to BackEnd API communication

The FrontEnd MVC controllers do not access SQL Server directly for these features. They call methods on `TutorFlowApiClient`, which sends HTTP requests to the configured API base URL (`https://localhost:7002/`). `BearerTokenHandler` reads the access token stored in the authenticated FrontEnd user's `access_token` claim and adds the `Authorization: Bearer` header. The BackEnd validates the JWT, checks the user's role, runs the matching service, and returns a JSON `ApiResponse` to the MVC controller for rendering in a Razor view.

The main request flow is:

```text
Browser -> FrontEnd MVC Controller -> TutorFlowApiClient -> BackEnd API Controller
       -> Business Service -> TutorFlowDbContext / SQL Server
Browser <- Razor View <- FrontEnd MVC Controller <- JSON ApiResponse <- BackEnd API
```

