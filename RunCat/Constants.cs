using System.Text.RegularExpressions;

namespace RunCat {
    /// <summary>
    /// This class contains some useful constant values.
    /// </summary>
    internal static class Constants {
        /// <summary>
        /// Name of process mutex.
        /// </summary>
        internal const string MUTEX = "_RUNCAT_MUTEX";

        /// <summary>
        /// String format of name of tray icon resources.
        /// </summary>
        /// <value>
        /// Name of tray icon resource items must follow the corresponding format: <c>`{0}_{1}_{2}`</c>.
        /// 
        /// <para><c>{0}</c>: The name of icon.</para>
        /// <para><c>{1}</c>: The name of system color theme. Allowed text is <c>Dark</c> and <c>Light</c>.</para>
        /// <para><c>{2}</c>: Frame nth number.</para>
        /// </value>
        /// <remarks>For example, <c>`Cat_Dark_2`</c> would be a correct form for this format.</remarks>
        internal const string TRAY_ICONS_NAME_FORMAT = @"{0}_{1}_{2}";

        /// <summary>
        /// Regular expression of name of tray icon resources.
        /// </summary>
        internal static readonly Regex TRAY_ICONS_NAME_REGEX = new Regex(@"^(\w+)_(Light|Dark)_(\d+)$");

        /// <summary>
        /// Performance counter information for <c>Processor - % Processor Time</c>.
        /// </summary>
        internal static readonly (string CategoryName, string CounterName) PROCESSOR_COUNTER_TIME = ("Processor", "% Processor Time");

        /// <summary>
        /// Performance counter information for <c>Processor Information - % Processor Utility</c>.
        /// </summary>
        internal static readonly (string CategoryName, string CounterName) PROCESSOR_COUNTER_UTILITY = ("Processor Information", "% Processor Utility");
    }
}
