using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MicroORM
{
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Загружает данные в DataTable.
        /// </summary>
        internal static async Task LoadAsync(this DataTable table, DbDataReader reader, CancellationToken cancellationToken)
        {
            bool columnsCreated = false;
            while(await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (columnsCreated)
                {
                }
                else
                {
                    columnsCreated = true;
                    CreateColumns(table, reader);
                }

                CreateRows(table, reader);
            }
        }

        /// <summary>
        /// Загружает данные в DataTable.
        /// </summary>
        internal static void LoadData(this DataTable table, DbDataReader reader)
        {
            bool columnsCreated = false;
            while (reader.Read())
            {
                if (columnsCreated)
                {
                }
                else
                {
                    columnsCreated = true;
                    CreateColumns(table, reader);
                }

                CreateRows(table, reader);
            }
        }

        private static void CreateRows(DataTable table, DbDataReader reader)
        {
            DataRow row = table.NewRow();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                object value = reader[i];
                row[i] = value;
            }
            table.Rows.Add(row);
        }

        private static void CreateColumns(DataTable table, DbDataReader reader)
        {
            var names = new HashSet<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                Type type = reader.GetFieldType(i);
                string name = reader.GetName(i);

                int n = 0;
                string origName = name;
                while(!names.Add(name))
                {
                    n++;
                    name = origName + n;
                }

                DataColumn column = table.Columns.Add(name, type);
            }
        }

        //internal static T CreateDelegate<T>(this MethodInfo method, object target) where T : Delegate
        //{
        //    Delegate deleg = method.CreateDelegate(typeof(T), target);
        //    return (T)deleg;
        //}

        internal static void AddRange<TCollection, TItem>(this TCollection collection, IList<TItem> items) where TCollection : ICollection<TItem>
        {
            for (int i = 0; i < items.Count; i++)
            {
                collection.Add(items[i]);
            }
        }
    }
}
