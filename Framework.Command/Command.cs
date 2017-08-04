using System;
using System.Data;

namespace Net.Framework.Command
{
    public partial class Command
    {
        internal CommandType CommandType { get; set; }
        internal string DbName { get; set; }
        internal string Text { get; set; }

        public T Read<T>(object paras = null, IDbTransaction transaction = null)
        {
            var type = typeof(T);
            var set = GetData(paras, transaction);
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Tuple<>) || type.GetGenericTypeDefinition() == typeof(Tuple<,>) || type.GetGenericTypeDefinition() == typeof(Tuple<,,>) || type.GetGenericTypeDefinition() == typeof(Tuple<,,,>) || type.GetGenericTypeDefinition() == typeof(Tuple<,,,,>) || type.GetGenericTypeDefinition() == typeof(Tuple<,,,,,>) || type.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,>) || type.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,,>)))
            {
                return (T)FillTuple(type, set, 0);
            }
            else
            {
                if (set.Tables.Count > 0)
                {
                    return (T)Fill(typeof(T), set.Tables[0]);
                }
                else
                {
                    return default(T);
                }
            }
        }

        public bool Exec(object paras = null, IDbTransaction transaction = null)
        {
            IDbConnection connection = null;
            if (transaction != null)
                connection = transaction.Connection;
            else
                connection = Commands.DBConnection(DbName);
            IDbCommand command = Commands.DBCommand(connection, CommandType, Text);

            SetParameters(command, paras);

            Commands.OpenDB(connection);
            try
            {
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception e)
            {
                transaction?.Rollback();
                throw e;
            }
            finally
            {
                if (transaction == null)
                    Commands.CloseDB(connection);
            }
        }
    }
}