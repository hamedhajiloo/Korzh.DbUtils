﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Korzh.DbUtils.SqlServer
{
    /// <summary>
    /// An implementation of <see cref="BaseDbBridge "/> for MySQL
    /// Implements the <see cref="Korzh.DbUtils.BaseDbBridge" />
    /// </summary>
    /// <seealso cref="Korzh.DbUtils.BaseDbBridge" />
    public class SqlServerBridge : BaseDbBridge
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerBridge"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public SqlServerBridge(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerBridge"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public SqlServerBridge(string connectionString, ILoggerFactory loggerFactory) 
            : base(connectionString, loggerFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerBridge"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public SqlServerBridge(SqlConnection connection) : base(connection)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerBridge"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public SqlServerBridge(SqlConnection connection, ILoggerFactory loggerFactory) 
            : base(connection, loggerFactory)
        {
        }

        /// <summary>
        /// Creates the connection.
        /// This is an abstract method which must be implemented in derived classes
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>DbConnection.</returns>
        protected override DbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        /// <summary>
        /// Gets the list of tables for the current DB and saves them to datasets list passed in the parameter.
        /// </summary>
        /// <param name="datasets">The list of datasets (tables) to fill.</param>
        protected override void ExtractDatasetList(IList<DatasetInfo> datasets)
        {
            DataTable schemaTable = Connection.GetSchema(SqlClientMetaDataCollectionNames.Tables);

            foreach (DataRow row in schemaTable.Rows) {
                string tableType = (string)row["TABLE_TYPE"];
                string tableName = (string)row["TABLE_NAME"];
                string tableSchema = (string)row["TABLE_SCHEMA"];
                if (tableType == "BASE TABLE") {
                    datasets.Add(new DatasetInfo(tableName, tableSchema));
                }
            }
        }

        /// <summary>
        /// Adds the parameters to the DB command object according to the current server type.
        /// </summary>
        /// <param name="command">The DB command.</param>
        /// <param name="record">The record. Each field in this record will be added a parameter.</param>
        protected override void AddParameters(IDbCommand command, IDataRecord record)
        {
  
            for (int i = 0; i < record.FieldCount; i++) {
                var parameter = new SqlParameter(ToParameterName(record.GetName(i)), record.GetValue(i))
                {
                    Direction = ParameterDirection.Input,
                    SqlDbType = record.GetFieldType(i).ToSqlDbType()
                };

                command.Parameters.Add(parameter);
            }
        }

        /// <summary>
        /// Sends an SQL command which turns off the constraints for the current table.
        /// Must be implemented in derived classes.
        /// </summary>
        protected override void TurnOffConstraints()
        {
            using (var command = GetConnection().CreateCommand()) {
                command.CommandText = $"ALTER TABLE {GetTableFullName(CurrentSeedingTable)} NOCHECK CONSTRAINT all";
                command.CommandType = CommandType.Text;

                Logger?.LogInformation(command.CommandText);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Sends an SQL command which turns the constraints on for the current table.
        /// Must be implemented in derived classes.
        /// </summary>
        protected override void TurnOnConstraints()
        {
            using (var command = GetConnection().CreateCommand()) {
                command.CommandText = $"ALTER TABLE {GetTableFullName(CurrentSeedingTable)} CHECK CONSTRAINT all";
                command.CommandType = CommandType.Text;

                Logger?.LogInformation(command.CommandText);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Sends an SQL command which turns off the possibility to set values for IDENTITY (auto-increment) columns for the current table.
        /// Must be implemented in derived classes.
        /// </summary>
        protected override void TurnOffAutoIncrement()
        {
            using (var command = GetConnection().CreateCommand()) {
                command.CommandText = $"IF EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = object_id('{GetTableFullName(CurrentSeedingTable)}') AND c.is_identity =1) begin SET IDENTITY_INSERT {GetTableFullName(CurrentSeedingTable)} ON end";
                command.CommandType = CommandType.Text;

                Logger?.LogInformation(command.CommandText);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Sends an SQL command which turns on the possibility to set values for IDENTITY (auto-increment) columns for the current table.
        /// Must be implemented in derived classes.
        /// </summary>
        protected override void TurnOnAutoIncrement()
        {
            using (var command = GetConnection().CreateCommand()) {
                command.CommandText = $"IF EXISTS (SELECT 1 from sys.columns c WHERE c.object_id = object_id('{GetTableFullName(CurrentSeedingTable)}') AND c.is_identity = 1) begin SET IDENTITY_INSERT {GetTableFullName(CurrentSeedingTable)} OFF end";
                command.CommandType = CommandType.Text;

                Logger?.LogInformation(command.CommandText);

                command.ExecuteNonQuery();
            }
        }
    }
}
