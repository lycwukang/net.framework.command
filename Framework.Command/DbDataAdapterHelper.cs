using System;
using System.Collections.Generic;
using System.Data;

namespace Net.Framework.Command
{
    internal class DbDataAdapterHelper
    {
        internal static Dictionary<string, Type> AdapterTypeDic = new Dictionary<string, Type>();
        internal static IDbDataAdapter GetDbDataAdapter(IDbCommand command)
        {
            var commandTypeFullName = command.GetType().FullName;
            if (!AdapterTypeDic.ContainsKey(commandTypeFullName))
            {
                lock (AdapterTypeDic)
                {
                    if (!AdapterTypeDic.ContainsKey(commandTypeFullName))
                    {
                        var adapterFullName = commandTypeFullName.Replace("Command", "DataAdapter");
                        var adapterType = command.GetType().Assembly.GetType(adapterFullName);
                        AdapterTypeDic.Add(commandTypeFullName, adapterType);
                    }
                }
            }
            return (IDbDataAdapter)Activator.CreateInstance(AdapterTypeDic[commandTypeFullName], command);
        }
    }
}