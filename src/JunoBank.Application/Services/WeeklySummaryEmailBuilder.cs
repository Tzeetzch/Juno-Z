using JunoBank.Application.DTOs;

namespace JunoBank.Application.Services;

public static class WeeklySummaryEmailBuilder
{
    public static string Build(string parentName, List<ChildWeeklySummary> children, DateTime weekStart, DateTime weekEnd)
    {
        var childSections = string.Join("", children.Select(BuildChildSection));

        return $"""
            <html>
            <body style="font-family: 'Segoe UI', sans-serif; background: #1a1a2e; color: #e0e0e0; padding: 24px; max-width: 600px; margin: 0 auto;">
                <div style="text-align: center; margin-bottom: 24px;">
                    <h1 style="color: #FF6B35; margin: 0;">Juno Bank</h1>
                    <p style="color: #999; margin: 4px 0 0;">Weekly Summary</p>
                </div>
                <p>Hi {HtmlEncode(parentName)},</p>
                <p>Here's what happened from <strong>{weekStart:dd MMM}</strong> to <strong>{weekEnd:dd MMM yyyy}</strong>:</p>
                {childSections}
                <hr style="border: none; border-top: 1px solid #333; margin: 24px 0;" />
                <p style="font-size: 12px; color: #666; text-align: center;">
                    Juno Bank &middot; Your family's virtual piggy bank
                </p>
            </body>
            </html>
            """;
    }

    private static string BuildChildSection(ChildWeeklySummary child)
    {
        var changeColor = child.BalanceChange >= 0 ? "#4caf50" : "#f44336";
        var changeSign = child.BalanceChange >= 0 ? "+" : "";

        var pendingNote = child.PendingRequests > 0
            ? $"<p style=\"color: #FFA726; font-size: 14px;\">{child.PendingRequests} pending request(s)</p>"
            : "";

        string transactionRows;
        if (child.Transactions.Count == 0)
        {
            transactionRows = """
                <tr><td colspan="3" style="padding: 12px; color: #666; text-align: center;">No transactions this week</td></tr>
                """;
        }
        else
        {
            transactionRows = string.Join("", child.Transactions.Select(t =>
            {
                var amountColor = t.IsDeposit ? "#4caf50" : "#f44336";
                var sign = t.IsDeposit ? "+" : "-";
                return $"""
                    <tr style="border-bottom: 1px solid #2a2a4e;">
                        <td style="padding: 8px; color: #999;">{t.CreatedAt:dd MMM}</td>
                        <td style="padding: 8px;">{HtmlEncode(t.Description)}</td>
                        <td style="padding: 8px; text-align: right; color: {amountColor};">{sign}&euro;{t.Amount:F2}</td>
                    </tr>
                    """;
            }));
        }

        return $"""
            <div style="background: #16213e; border-radius: 8px; padding: 16px; margin-bottom: 16px;">
                <h2 style="color: #9B59B6; margin: 0 0 8px;">{HtmlEncode(child.Name)}</h2>
                <p style="margin: 4px 0;">
                    Balance: <strong style="color: #FF6B35;">&euro;{child.Balance:F2}</strong>
                    <span style="color: {changeColor}; font-size: 14px; margin-left: 8px;">({changeSign}&euro;{Math.Abs(child.BalanceChange):F2})</span>
                </p>
                {pendingNote}
                <table width="100%" cellpadding="0" cellspacing="0" style="border-collapse: collapse; margin-top: 8px;">
                    <thead>
                        <tr style="background: #2a2a4e;">
                            <th style="padding: 8px; text-align: left; color: #999; font-weight: normal;">Date</th>
                            <th style="padding: 8px; text-align: left; color: #999; font-weight: normal;">Description</th>
                            <th style="padding: 8px; text-align: right; color: #999; font-weight: normal;">Amount</th>
                        </tr>
                    </thead>
                    <tbody>
                        {transactionRows}
                    </tbody>
                </table>
            </div>
            """;
    }

    private static string HtmlEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
