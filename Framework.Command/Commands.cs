using System;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;

namespace Net.Framework.Command
{
    public static class Commands
    {
        public static Command GetCommand(string sql, string dbName, CommandType type = CommandType.Text)
        {
            return new Command()
            {
                CommandType = type,
                Text = sql,
                DbName = dbName
            };
        }

        public static IDbTransaction BeginTransaction(string dbName)
        {
            IDbConnection connection = DBConnection(dbName);
            OpenDB(connection);
            return connection.BeginTransaction();
        }

        /// <summary>
        /// 创建动态参数
        /// </summary>
        /// <returns></returns>
        public static dynamic GetDynamicParams()
        {
            return new ExpandoObject();
        }

        internal static void OpenDB(IDbConnection connection)
        {
            if (connection.State == ConnectionState.Broken)
                connection.Close();
            if (connection.State == ConnectionState.Closed)
                connection.Open();
        }

        internal static void CloseDB(IDbConnection connection)
        {
            if (connection.State == ConnectionState.Open || connection.State == ConnectionState.Broken)
                connection.Close();
        }

        internal static IDbConnection DBConnection(string dbName)
        {
            var providerName = ConnectionConfigHelper.GetConfigProviderName(dbName);

            var type = string.IsNullOrEmpty(providerName) ? typeof(SqlConnection) : Type.GetType(providerName);

            var connection = Activator.CreateInstance(type, ConnectionConfigHelper.GetConfigConnection(dbName));
            return connection as IDbConnection;
        }

        internal static IDbCommand DBCommand(IDbConnection connection, CommandType commandType, string text)
        {
            var command = connection.CreateCommand();
            command.CommandType = commandType;
            command.CommandText = text;
            return command;
        }
    }
}