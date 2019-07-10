﻿namespace Korzh.DbUtils.MySql
{
    public static class DbMySqlBridgeExtensions
    {
        public static void UseMySQL(this DbInitializerOptions options, string connectionString)
        {
            options.DbWriter = new MySqlBride(connectionString);
        }
    }
}
