namespace Mci.Core.Domain.Enums;

public enum ImportRunStatus
{
    Queued,
    Running,
    Succeeded,
    SucceededWithWarnings,
    Failed
}
