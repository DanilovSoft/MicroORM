using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
            var columnsCreated = false;
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
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
            var columnsCreated = false;
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
            var row = table.NewRow();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader[i];
                row[i] = value;
            }
            table.Rows.Add(row);
        }

        private static void CreateColumns(DataTable table, DbDataReader reader)
        {
            var names = new HashSet<string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var type = reader.GetFieldType(i);
                var name = reader.GetName(i);

                var n = 0;
                var origName = name;
                while(!names.Add(name))
                {
                    n++;
                    name = origName + n;
                }

                var column = table.Columns.Add(name, type);
            }
        }

        //internal static T CreateDelegate<T>(this MethodInfo method, object target) where T : Delegate
        //{
        //    Delegate deleg = method.CreateDelegate(typeof(T), target);
        //    return (T)deleg;
        //}

        internal static void AddRange<TCollection, TItem>(this TCollection collection, IList<TItem> items) where TCollection : ICollection<TItem>
        {
            for (var i = 0; i < items.Count; i++)
            {
                collection.Add(items[i]);
            }
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static string SnakeToPascalCase(this string str)
        {
            var newLength = str.Length;

            for (var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                if (ch == '_' || ch == ' ')
                {
                    --newLength;
                }
            }

            return string.Create(newLength, str, (span, s) =>
            {
                var wordStart = true;
                var spanIndex = 0;
                for (var i = 0; i < s.Length; i++)
                {
                    var ch = s[i];
                    if (ch == '_' || ch == ' ')
                    {
                        wordStart = true;
                    }
                    else
                    {
                        span[spanIndex++] = wordStart ? char.ToUpperInvariant(ch) : ch;
                        wordStart = false;
                    }
                }
            });
        }

        public static string ToLowerFirstLetter(this string str)
        {
            if (str != null && str.Length > 0)
            {
                return string.Create(str.Length, str, (span, s) =>
                {
                    span[0] = char.ToLowerInvariant(s[0]);
                    s.AsSpan(1).CopyTo(span[1..]);
                });
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(str));
            }
        }
    }
}

namespace System.Threading.Tasks
{
    internal static class ExtensionMethods
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsCompletedSuccessfully(this Task task)
        {
#if !NETSTANDARD2_0
            return task.IsCompletedSuccessfully;
#else
            return task.Status == TaskStatus.RanToCompletion;
#endif
        }
    }
}
