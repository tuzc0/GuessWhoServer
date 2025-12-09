using System.Data.SqlClient;

namespace GuessWhoTests.Utilities
{
    public static class DatabaseResetter
    {
        // Conexión a 'master' (necesaria para borrar/restaurar la otra BD)
        private const string MasterConnectionString = "Server=localhost\\SQLEXPRESS;Database=master;Integrated Security=True;";

        // Ruta del archivo que creamos en el Paso 1
        private const string BackupPath = @"D:\Juegos\MSSQL16.SQLEXPRESS\MSSQL\DATA\GuessWho_CleanState.bak";

        // El nombre EXACTO de tu base de datos
        private const string DbName = "GuessWhoDB_Test";

        public static void ResetDatabase()
        {
            string sql = $@"
                USE [master];

                -- 1. Si la BD existe, la ponemos en modo SINGLE_USER para sacar a todos
                IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{DbName}')
                BEGIN
                    ALTER DATABASE [{DbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                END

                -- 2. Restauramos usando el backup limpio (Sobreescribe todo)
                RESTORE DATABASE [{DbName}] 
                FROM DISK = '{BackupPath}' 
                WITH REPLACE, RECOVERY;

                -- 3. Regresamos la BD a modo MULTI_USER para que las pruebas entren
                ALTER DATABASE [{DbName}] SET MULTI_USER;
            ";

            using (var connection = new SqlConnection(MasterConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = 60;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}