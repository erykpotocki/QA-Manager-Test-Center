using System;
using System.Collections.Generic;
using System.Linq;

namespace QARegressionManager.Services;

public sealed class SessionSummaryService
{
    public SessionSummary Calculate(
        IEnumerable<string> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        var statusList = statuses.ToList();

        var success = statusList.Count(
            status => status == "Success");

        var failed = statusList.Count(
            status => status == "Failed");

        var notApplicable = statusList.Count(
            status => status == "NA");

        var blocked = statusList.Count(
            status => status == "Blocked");

        var remaining = statusList.Count(
            status => status == "None");

        return new SessionSummary(
            success,
            failed,
            notApplicable,
            blocked,
            remaining,
            statusList.Count);
    }
}

public sealed record SessionSummary(
    int Success,
    int Failed,
    int NotApplicable,
    int Blocked,
    int Remaining,
    int Total)
{
    public int Completed =>
        Success +
        Failed +
        NotApplicable +
        Blocked;

    public bool IsCompleted =>
        Remaining == 0;

    public string ResultType
    {
        get
        {
            if (!IsCompleted)
            {
                return "InProgress";
            }

            if (Failed > 0)
            {
                return "Failed";
            }

            if (Blocked > 0)
            {
                return "Blocked";
            }

            if (NotApplicable > 0)
            {
                return "NA";
            }

            return "Success";
        }
    }
}