# TutorFlow – UI Design Specification

Design brief for the TutorFlow tutoring-center management web UI. Use this file as the source of truth for any AI design tool or for the frontend developers.

- **UI language:** Vietnamese (all visible text). This document is in English.
- **Frontend approach:** default is ASP.NET Core MVC (`.cshtml` views) calling the REST API through `HttpClient`. If the team chooses plain HTML + JS, see section 9.
- **CSS framework:** Bootstrap 5.3 (already in `wwwroot/lib`), customised with CSS variables.

---

## 1. Users and roles

| Role | Vietnamese | Scope |
|---|---|---|
| Admin | Quản trị viên | Whole system, accounts, facility managers, dashboard |
| GrandManager | Quản lý tổng | Classes, sessions, enrollment, tuition, payments |
| FacilityManager | Quản lý cơ sở | Rooms and change requests of **one** facility |
| Lecturer | Giảng viên | Own schedule, attendance, change requests |
| Student | Học viên | Own schedule, attendance, tuition (read-only) |

Each role gets its own sidebar menu. Hiding a menu item is cosmetic only; the backend enforces permissions.

---

## 2. Visual language

### 2.1 Principles
1. Calm, clear, professional. Data first, decoration last.
2. Status is always **colour + text** (badge), never colour alone.
3. Desktop-first for managers, mobile-first for Lecturer and Student screens.
4. Conflicts and errors explain *what* collided and *what to do next*.

### 2.2 Design tokens

```css
:root {
  --tf-primary:       #3B5BDB;
  --tf-primary-hover: #364FC7;
  --tf-primary-soft:  #EDF2FF;

  --tf-success: #2F9E44;  --tf-success-soft: #EBFBEE;
  --tf-warning: #E67700;  --tf-warning-soft: #FFF4E6;
  --tf-danger:  #E03131;  --tf-danger-soft:  #FFF5F5;
  --tf-neutral: #868E96;  --tf-neutral-soft: #F1F3F5;

  --tf-bg:        #F8F9FA;
  --tf-surface:   #FFFFFF;
  --tf-border:    #DEE2E6;
  --tf-text:      #212529;
  --tf-text-muted:#6C757D;

  --tf-radius:    10px;
  --tf-radius-sm: 6px;
  --tf-shadow:    0 1px 2px rgba(0,0,0,.06);
  --tf-space:     8px;            /* 8px grid */
}
/* Dark mode: redefine the same tokens under [data-theme="dark"] */
```

### 2.3 Typography
- Font: **Be Vietnam Pro** or **Inter** (must render Vietnamese diacritics correctly).
- Scale: 12 / 14 (body) / 16 / 20 / 24 / 32 px. Weights 400 and 600.
- Line height 1.5 for body, 1.25 for headings.

### 2.4 Status colour map

| Meaning | Colour | Examples |
|---|---|---|
| Success / positive | Green | Present, Paid, Approved, Completed, Active |
| Warning / waiting | Amber | Pending, Unpaid (not overdue), Scheduled |
| Danger / blocked | Red | UnexcusedAbsent, Closed, Rejected, Conflict, Overdue |
| Neutral / empty | Gray | Empty slot, Cancelled, Dropped |
| Info / selected | Blue | Selected row, ExcusedAbsent |

---

## 3. Layout shell

```
┌──────────┬───────────────────────────────────────────┐
│          │ Topbar: page title · facility badge ·      │
│ Sidebar  │         bell · user menu                   │
│ (per     ├───────────────────────────────────────────┤
│  role)   │ Breadcrumb                                 │
│          │ Page header (title + primary action)       │
│          │ Content                                    │
└──────────┴───────────────────────────────────────────┘
```

- Sidebar 248px, collapses to an off-canvas drawer below 992px.
- Facility badge in the topbar appears only for FacilityManager (read from the `FacilityName` claim).
- Content max width 1280px; 24px page padding (16px on mobile).

### Sidebar menus

| Role | Items |
|---|---|
| Admin | Dashboard · Người dùng · Gán quản lý cơ sở |
| GrandManager | Lớp học · Ghi danh · Học phí · Người dùng (FM, Lecturer, Student) |
| FacilityManager | Lưới phòng · Yêu cầu đổi phòng/ca · Bảo trì phòng |
| Lecturer | Lịch dạy · Điểm danh · Yêu cầu đổi phòng/ca |
| Student | Lịch học · Điểm danh của tôi · Học phí |

---

## 4. Component library

Design these first as a style-guide page.

| Component | Notes |
|---|---|
| Stat card | Icon, label, big number, optional trend |
| Data table | Search box, sortable headers (arrow shows direction), **secondary "then by" sort dropdown**, pagination, page-size select; collapses to cards on mobile |
| Filter bar | Dropdowns + search + "Xóa bộ lọc" |
| Status badge | Pill, 12px, soft background + strong text |
| Form field | Label, hint, inline validation message, required marker |
| Confirm modal | Title, consequence text, danger variant |
| Toast | Success / error / info, auto-dismiss 5s, error stays until closed |
| Conflict table | Columns: Ngày · Loại xung đột · Lớp đang chiếm (see 5.5) |
| Weekday chips | T2 … T7, CN; toggle buttons |
| Segmented control | 3 options for attendance |
| File upload box | Drag-and-drop, accepts `.xlsx`, shows row-level import errors |
| Export button | "Xuất Excel" with spinner state |
| Empty state | Illustration-free: icon, one sentence, one action |
| Skeleton | Table rows and cards while loading |
| Error banner | Full-width, dismissible, shows error code in small muted text |

States required for list, grid and attendance screens: **default · loading · empty · error**.

---

## 5. Screens

### 5.1 Public

| # | Screen | Key elements |
|---|---|---|
| 1 | Đăng nhập | Email, password, remember me, "Quên mật khẩu?", error banner, lockout message |
| 2 | Đăng ký học viên | Full name, email, phone, password, confirm password |
| 3 | Quên / đặt lại mật khẩu | Email form, then new-password form (token in URL) |
| 4 | Từ chối truy cập (403) | Short message, "Về trang chủ" |

### 5.2 Admin

| # | Screen | Key elements |
|---|---|---|
| 5 | Dashboard | 8 stat cards: total rooms, open rooms, closed rooms, active classes, active lecturers, active students, pending change requests, total unpaid tuition (VNĐ). Bar chart "Phòng theo cơ sở". List of latest pending requests |
| 6 | Quản lý người dùng | Table, role filter, search, active/locked toggle, soft delete, Import/Export Excel |
| 7 | Tạo / sửa người dùng | Full name, email, phone, role, temporary password; reset-password action |
| 8 | Gán quản lý cơ sở | 3 facility cards, each with manager dropdown; error shown if role is wrong |

### 5.3 Grand Manager

| # | Screen | Key elements |
|---|---|---|
| 9 | Danh sách lớp | Filters: subject, lecturer, facility, status; "Tạo lớp" |
| 10 | Tạo / sửa lớp | See 5.5 |
| 11 | Chi tiết lớp | Tabs: Buổi học · Học viên · Học phí. Actions: đổi giảng viên, dạy thay, hủy buổi, đóng lớp |
| 12 | Ghi danh | Student search → choose class → conflict preview → confirm. Drop / re-enroll. Excel import |
| 13 | Học phí | Filter by class + period (default: current month). Columns: student, Present, ExcusedAbsent, UnexcusedAbsent, amount. "Phát hành học phí". Payment drawer. Export Excel |

### 5.4 Facility Manager

| # | Screen | Key elements |
|---|---|---|
| 14 | Lưới phòng × ca | Date picker; rows = rooms, columns = 3 shifts. Cell states below |
| 15 | Đóng phòng bảo trì | Confirm modal; if blocked, list of affected sessions with per-row actions "Dời sang phòng trống" / "Hủy buổi" |
| 16 | Hộp yêu cầu đổi phòng/ca | Pending list with count badge; detail panel (lecturer, class, current vs requested room/shift, reason). Approve; Reject requires a note. On failure the request stays Pending and the reason is shown |

**Room grid cell states**

| State | Look | Content |
|---|---|---|
| Occupied | Blue-soft | Class name + lecturer |
| Empty | White, dashed border | "+ Xếp phòng" on hover |
| Closed for Maintenance | Red-soft with hatching | "Đóng bảo trì" |

### 5.5 Create class form with conflict table (hero screen)

Fields: class name, subject, lecturer, facility, default room (open rooms of the chosen facility only), shift, weekday chips, start date, end date.

Shifts (real data):

| Shift | Time |
|---|---|
| Ca 1 | 04:30 – 06:00 |
| Ca 2 | 06:00 – 07:30 |
| Ca 3 | 07:30 – 09:00 |

Behaviour:
- A live preview lists all generated session dates (from start to end, matching the weekdays).
- **Clean:** green banner "Không có xung đột. Sẽ tạo N buổi học."
- **Conflicts:** red banner "Không có buổi học nào được tạo" plus a conflict table.

| Ngày | Loại xung đột | Lớp đang chiếm |
|---|---|---|
| 05/01/2027 | Phòng đã có lớp | Toán 9A – Ca 1 |
| 07/01/2027 | Giảng viên trùng ca | Lý 10B – Cơ sở 2 |
| 12/01/2027 | Phòng đang đóng bảo trì | – |

The submit button is disabled while conflicts exist.

### 5.6 Lecturer

| # | Screen | Key elements |
|---|---|---|
| 17 | Lịch dạy của tôi | Week view (Mon–Sun × 3 shifts) with a day-list toggle; session cards with Scheduled / Completed / Cancelled badges |
| 18 | Điểm danh | Header (class, room, shift, date). Roster with 3-way segmented control per student: **Có mặt / Nghỉ có phép / Nghỉ không phép**; note field; "Chọn tất cả có mặt"; sticky bottom bar with "Lưu" and "Hoàn tất buổi học". Read-only once completed |
| 19 | Yêu cầu đổi phòng/ca | Pick a session or "cả lớp"; requested room and/or shift (at least one required); reason; "Yêu cầu của tôi" list with status badges |

### 5.7 Student (read-only)

| # | Screen | Key elements |
|---|---|---|
| 20 | Lịch học | Week / list toggle |
| 21 | Điểm danh của tôi | Per-class summary (Present / Excused / Unexcused) + history |
| 22 | Học phí của tôi | Period, amount, Paid / Unpaid, payment history |

---

## 6. Error and message catalogue (Vietnamese)

Map API error codes to friendly text. Show as toast or banner; keep the code in small muted text.

| Code | Message |
|---|---|
| `ROOM_CONFLICT` | Phòng đã có lớp khác vào ngày và ca này. |
| `LECTURER_CONFLICT` | Giảng viên đã có lớp khác cùng ngày và ca. |
| `SESSION_DUPLICATE` | Lớp này đã có buổi học trong ngày đó. |
| `ROOM_CLOSED` | Phòng đang đóng bảo trì, không thể xếp lịch. |
| `ROOM_HAS_SCHEDULE` | Phòng còn buổi học sắp tới. Hãy dời hoặc hủy các buổi đó trước khi đóng phòng. |
| `CONFLICT_STUDENT_SESSION` | Thay đổi này làm trùng lịch của học viên đang theo học. |
| `CONFLICT_STUDENT_ENROLL` | Học viên đã có lớp khác trùng ca và ngày. |
| `INVALID_ROLE` | Tài khoản được chọn không đúng vai trò. |
| `ATTENDANCE_NOT_ENROLLED` | Học viên chưa ghi danh vào lớp này. |
| `ATTENDANCE_FORBIDDEN` | Chỉ giảng viên phụ trách buổi học mới được điểm danh. |
| `ATTENDANCE_SESSION_INVALID` | Không thể điểm danh buổi đã hủy hoặc chưa diễn ra. |

---

## 7. Responsive and accessibility

- Breakpoints: 576 / 768 / 992 / 1200 px.
- Tables become stacked cards below 768px. Room grid scrolls horizontally with a sticky first column.
- Attendance screen: touch targets at least 44px, sticky action bar, usable one-handed.
- Contrast at least 4.5:1, visible focus ring, every input has a label, full keyboard navigation, modals trap focus.
- Dates `dd/MM/yyyy`, times 24h, money `1.400.000 ₫`.

---

## 8. Deliverables checklist

- [ ] Style guide page (tokens, type scale, components, states)
- [ ] All screens in section 5, desktop
- [ ] Mobile versions of: create class (10), room grid (14), attendance (18)
- [ ] Loading, empty and error states for list, grid and attendance
- [ ] Dark-mode tokens (optional)

---

## 9. Frontend approach options

| Option | How | Notes |
|---|---|---|
| A. MVC + Razor (default) | `Controller` calls the API with `HttpClient`, renders `.cshtml`; JWT kept in an `HttpOnly` cookie | Matches the current `TutorFlow.Web` project |
| B. Plain HTML + JS | Static pages in `wwwroot`, data loaded with `fetch()` from the API | Keep sample data in `mock-data.js` until the API is ready |

For option B, ask the design tool to output standalone `.html` files with Bootstrap 5.3 from a CDN and vanilla JavaScript, with no server-side templating.

---

## 10. Prompt to paste into a design tool

> Design the complete web UI for "TutorFlow", a tutoring-center management system, following the specification in this document (sections 2–8). Output responsive HTML and CSS using Bootstrap 5.3. All visible text in Vietnamese with realistic sample data. Start with the style guide, then the screens role by role. The create-class form with the conflict table (5.5), the room × shift grid (14) and the attendance screen (18) are the hero screens and need desktop and mobile versions.
