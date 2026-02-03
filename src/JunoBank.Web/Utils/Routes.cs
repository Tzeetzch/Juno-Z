namespace JunoBank.Web.Utils;

/// <summary>
/// Centralized URL helper methods to avoid magic strings throughout the app.
/// </summary>
public static class AppRoutes
{
    public static class Child
    {
        public const string Dashboard = "/child/dashboard";
        public const string RequestDeposit = "/child/request-deposit";
        public const string RequestWithdrawal = "/child/request-withdrawal";
    }

    public static class Parent
    {
        public const string Dashboard = "/parent";
        public const string PendingRequests = "/parent/requests";
        public const string TransactionHistory = "/parent/history";
        public const string Settings = "/parent/settings";

        /// <summary>
        /// Generates the child detail URL for a specific child.
        /// </summary>
        public static string ChildDetail(int childId) => $"/parent/child/{childId}";

        /// <summary>
        /// Generates the pending requests URL for a specific child.
        /// </summary>
        public static string ChildRequests(int childId) => $"/parent/child/{childId}/requests";

        /// <summary>
        /// Generates the request history URL for a specific child.
        /// </summary>
        public static string ChildRequestHistory(int childId) => $"/parent/child/{childId}/request-history";

        /// <summary>
        /// Generates the transaction history URL for a specific child.
        /// </summary>
        public static string ChildTransactionHistory(int childId) => $"/parent/child/{childId}/transactions";

        /// <summary>
        /// Generates the manual transaction URL for a specific child.
        /// </summary>
        public static string ChildTransaction(int childId) => $"/parent/child/{childId}/transaction";

        /// <summary>
        /// Generates the settings URL for a specific child.
        /// </summary>
        public static string ChildSettings(int childId) => $"/parent/child/{childId}/settings";

        /// <summary>
        /// Generates the URL to create a new standing order for a child.
        /// </summary>
        public static string ChildOrderNew(int childId) => $"/parent/child/{childId}/order/new";

        /// <summary>
        /// Generates the URL to edit a standing order for a child.
        /// </summary>
        public static string ChildOrderEdit(int childId, int orderId) => $"/parent/child/{childId}/order/{orderId}";
    }

    public static class Auth
    {
        public const string Login = "/login";
        public const string ParentLogin = "/login/parent";
    }

    public static class Setup
    {
        public const string Wizard = "/setup";
    }
}
