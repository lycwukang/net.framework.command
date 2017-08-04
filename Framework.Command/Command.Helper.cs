using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Net.Framework.Command
{
    public partial class Command
    {
        private static object ConvertAndFillValue(Type type, object value)
        {
            var baseType = type;
            if (baseType.IsGenericType && (baseType.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                baseType = baseType.GetGenericArguments()[0];
            }
            var obj = value;

            if (baseType == typeof(string)) obj = Convert.ToString(value);
            else if (baseType == typeof(int)) obj = Convert.ToInt32(value);
            else if (baseType == typeof(decimal)) obj = Convert.ToDecimal(value);
            else if (baseType == typeof(long)) obj = Convert.ToInt64(value);
            else if (baseType == typeof(bool)) obj = Convert.ToBoolean(value);
            else if (baseType == typeof(DateTime)) obj = Convert.ToDateTime(value);
            else if (baseType == typeof(double)) obj = Convert.ToDouble(value);
            else if (baseType == typeof(float)) obj = Convert.ToSingle(value);
            else if (baseType == typeof(short)) obj = Convert.ToInt16(value);
            else if (baseType.IsEnum) obj = Enum.ToObject(baseType, value);

            return obj;
        }

        private static void FillParamster(IDbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        private static void SetParameters(IDbCommand command, object paras)
        {
            if (paras == null) return;

            if (paras is IDictionary<string, object>)
            {
                foreach (var pair in (paras as IDictionary<string, object>))
                {
                    FillParamster(command, pair.Key, pair.Value);
                }
            }
            else
            {
                PropertyInfo[] propertys = paras.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var property in propertys)
                {
                    if (property.CanRead)
                    {
                        FillParamster(command, property.Name, property.GetValue(paras));
                    }
                }
            }
        }

        private DataSet GetData(object paras, IDbTransaction transaction)
        {
            IDbConnection connection = null;
            if (transaction != null)
                connection = transaction.Connection;
            else
                connection = Commands.DBConnection(DbName);
            IDbCommand command = Commands.DBCommand(connection, CommandType, Text);

            SetParameters(command, paras);

            IDbDataAdapter adapter = DbDataAdapterHelper.GetDbDataAdapter(command);
            DataSet ds = new DataSet();
            Commands.OpenDB(connection);
            try
            {
                adapter.Fill(ds);
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
            return ds;
        }

        private object Fill(Type type, DataTable table)
        {
            #region 集合类型
            if (type.IsClass && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var arrayObj = Activator.CreateInstance(type);
                var AddMethod = type.GetMethod("Add");
                var gengerType = type.GenericTypeArguments[0];
                if (gengerType.IsValueType || gengerType == typeof(string))
                {
                    foreach (DataRow row in table.Rows)
                    {
                        if (!Convert.IsDBNull(row[0])) AddMethod.Invoke(arrayObj, new object[] { ConvertAndFillValue(gengerType, row[0]) });
                        else
                        {
                            if (gengerType.IsValueType)
                            {
                                AddMethod.Invoke(arrayObj, new object[] { Activator.CreateInstance(gengerType) });
                            }
                            else
                            {
                                AddMethod.Invoke(arrayObj, new object[] { null });
                            }
                        }
                    }
                }
                else
                {
                    PropertyInfo[] propertys = gengerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (DataRow row in table.Rows)
                    {
                        var obj = Activator.CreateInstance(gengerType);
                        foreach (PropertyInfo property in propertys)
                        {
                            if (property.CanWrite && table.Columns.Contains(property.Name))
                            {
                                object value = row[property.Name];
                                if (value != null && !Convert.IsDBNull(value))
                                {
                                    property.SetValue(obj, ConvertAndFillValue(property.PropertyType, value));
                                }
                            }
                        }
                        AddMethod.Invoke(arrayObj, new object[] { obj });
                    }
                }
                return arrayObj;
            }
            #endregion
            #region 单记录类型
            if (type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) || type == typeof(string))
            {
                if (table.Rows.Count > 0 && table.Columns.Count > 0)
                {
                    var value = table.Rows[0][0];
                    if (!Convert.IsDBNull(value)) return ConvertAndFillValue(type, value);
                }
            }
            #endregion
            #region 对象类型
            if (type.IsClass && !type.IsGenericType)
            {
                if (table.Rows.Count > 0 && table.Columns.Count > 0)
                {
                    PropertyInfo[] propertys = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    var row = table.Rows[0];
                    var obj = Activator.CreateInstance(type);
                    foreach (PropertyInfo property in propertys)
                    {
                        if (property.CanWrite && table.Columns.Contains(property.Name))
                        {
                            object value = row[property.Name];
                            if (value != null && !Convert.IsDBNull(value))
                            {
                                property.SetValue(obj, ConvertAndFillValue(property.PropertyType, value));
                            }
                        }
                    }
                    return obj;
                }
            }
            #endregion

            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private object FillTuple(Type type, DataSet set, int deviation)
        {
            if (type.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,,>))
            {
                var tupleParms = new object[type.GetGenericArguments().Length];
                var objectTypes = new Type[type.GetGenericArguments().Length - 1];
                for (int i = 0; i < objectTypes.Length; i++)
                {
                    objectTypes[i] = type.GetGenericArguments()[i];
                }
                var objects = FillOjbect(objectTypes, set, deviation);
                for (int i = 0; i < objects.Length; i++)
                {
                    tupleParms[i] = objects[i];
                }
                tupleParms[type.GetGenericArguments().Length - 1] = FillTuple(type.GetGenericArguments()[type.GetGenericArguments().Length - 1], set, deviation + objects.Length);
                return Activator.CreateInstance(type, tupleParms);
            }
            else
            {
                var tupleParams = FillOjbect(type.GetGenericArguments(), set, deviation);
                return Activator.CreateInstance(type, tupleParams);
            }
        }

        private object[] FillOjbect(Type[] types, DataSet set, int deviation)
        {
            var objects = new object[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                DataTable table = set.Tables[i + deviation];
                objects[i] = Fill(types[i], table);
            }
            return objects;
        }
    }
}