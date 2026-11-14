namespace BackEnd.Services.Tuition;

public static class TuitionPaymentStatus
{
    public const string Unpaid = "Unpaid";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Paid = "Paid";
    public const string Overdue = "Overdue";

    public static string GetLedgerStatus(decimal total, decimal paid) =>
        paid >= total ? Paid : paid > 0 ? PartiallyPaid : Unpaid;

    public static string GetDisplayStatus(decimal total, decimal paid, DateOnly? dueDate, DateOnly today) =>
        paid < total && dueDate.HasValue && dueDate.Value < today
            ? Overdue
            : GetLedgerStatus(total, paid);
}