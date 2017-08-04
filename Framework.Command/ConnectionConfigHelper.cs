using System.Configuration;

namespace Net.Framework.Command
{
    internal class ConnectionConfigHelper
    {
        internal static string GetConfigConnection(string name)
        {
            return ConfigurationManager.ConnectionStrings[name].ConnectionString;
        }

        internal static string GetConfigProviderName(string name)
        {
            return ConfigurationManager.ConnectionStrings[name].ProviderName;
        }
    }
}