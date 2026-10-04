
namespace CMS.Domain.Enums;

public enum PayrollAdjustmentKind { Addition = 1, Deduction = 2 }

public enum PayrollAdjustmentType
{
    // Deduction types
    LoanInstallment = 1,
    Absence = 2,
    Penalty = 3,
    Insurance = 4,

    // Addition types
    Incentive = 20,
    Bonus = 21,
    Commission = 22,
    Overtime = 23,

    Other = 99
}

public enum InstallmentStatus
{
    Pending = 1,
    Deducted = 2,
    Skipped = 3
}
