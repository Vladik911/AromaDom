using System.Data;
using System.IO;
using System.Runtime.InteropServices;
namespace AromaDom;
public static class Database
{
    public static string PathName { get; } = Path.Combine(Environment.GetEnvironmentVariable("AROMADOM_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AromaDom"), "aroma.db");
    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, out IntPtr db);
    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_close(IntPtr db);
    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_prepare_v2(IntPtr db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int length, out IntPtr statement, IntPtr tail);
    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_step(IntPtr statement);
    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_finalize(IntPtr statement);
    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_column_count(IntPtr statement);
    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr sqlite3_column_name(IntPtr statement, int index);
    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr sqlite3_column_text(IntPtr statement, int index);
    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr sqlite3_errmsg(IntPtr db);
    public static void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
        if (!File.Exists(PathName))
            File.Copy(Path.Combine(AppContext.BaseDirectory, "aroma.db"), PathName);
    }
    public static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
    public static DataTable Query(string sql)
    {
        if (sqlite3_open(PathName, out var db) != 0)
            throw new Exception("Не удалось открыть базу данных.");
        try
        {
            Run(db, "PRAGMA foreign_keys=ON");
            return Run(db, sql);
        }
        finally
        {
            sqlite3_close(db);
        }
    }
    static DataTable Run(IntPtr db, string sql)
    {
        if (sqlite3_prepare_v2(db, sql, -1, out var statement, IntPtr.Zero) != 0)
            throw new Exception(Marshal.PtrToStringUTF8(sqlite3_errmsg(db)));
        try
        {
            var table = new DataTable();
            var count = sqlite3_column_count(statement);
            for (var i = 0; i < count; i++)
                table.Columns.Add(Marshal.PtrToStringUTF8(sqlite3_column_name(statement, i))!);
            int result;
            while ((result = sqlite3_step(statement)) == 100)
            {
                var values = new object[count];
                for (var i = 0; i < count; i++)
                    values[i] = Marshal.PtrToStringUTF8(sqlite3_column_text(statement, i)) ?? "";
                table.Rows.Add(values);
            }
            if (result != 101)
                throw new Exception(Marshal.PtrToStringUTF8(sqlite3_errmsg(db)));
            return table;
        }
        finally
        {
            sqlite3_finalize(statement);
        }
    }

    public static void ExecuteTransaction(IEnumerable<string> statements)
    {
        if (sqlite3_open(PathName, out var db) != 0)
            throw new Exception("Не удалось открыть базу данных.");
        try
        {
            Run(db, "PRAGMA foreign_keys=ON");
            Run(db, "BEGIN IMMEDIATE");
            try
            {
                foreach (var sql in statements)
                    Run(db, sql);
                Run(db, "COMMIT");
            }
            catch
            {
                Run(db, "ROLLBACK");
                throw;
            }
        }
        finally
        {
            sqlite3_close(db);
        }
    }
}

