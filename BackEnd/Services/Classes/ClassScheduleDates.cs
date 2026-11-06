using BackEnd.Dtos.Classes;

namespace BackEnd.Services.Classes;

internal static class ClassScheduleDates
{
    public static bool TryCreate(ClassWriteDto request, out List<DateOnly> dates, out string error)
    {
        dates = new List<DateOnly>();
        error = string.Empty;

        if (request.StartDate == default || request.EndDate == default || request.EndDate < request.StartDate)
        {
            error = "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.";
            return false;
        }

        var days = new HashSet<int>();
        foreach (var token in (request.DaysOfWeek ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(token, out var day) || day is < 2 or > 8)
            {
                error = "Ngày học trong tuần phải là các số từ 2 đến 8 (Thứ 2 đến Chủ nhật), phân tách bằng dấu phẩy.";
                return false;
            }

            days.Add(day);
        }

        if (days.Count == 0)
        {
            error = "Vui lòng chọn ít nhất một ngày học trong tuần.";
            return false;
        }

        var current = request.StartDate;
        while (true)
        {
            if (days.Contains(ToProjectDay(current.DayOfWeek)))
            {
                dates.Add(current);
            }

            if (current == request.EndDate)
            {
                break;
            }

            current = current.AddDays(1);
        }

        if (dates.Count == 0)
        {
            error = "Khoảng ngày đã chọn không có ngày nào khớp với lịch học trong tuần.";
            return false;
        }

        return true;
    }

    public static int ToProjectDay(DayOfWeek day) => day == DayOfWeek.Sunday ? 8 : (int)day + 1;

    public static string ShiftWeekDays(string daysOfWeek, int delta)
    {
        var shifted = daysOfWeek.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(token => int.Parse(token))
            .Select(day => ((day - 2 + delta) % 7 + 7) % 7 + 2)
            .Distinct()
            .OrderBy(day => day);
        return string.Join(',', shifted);
    }
}
