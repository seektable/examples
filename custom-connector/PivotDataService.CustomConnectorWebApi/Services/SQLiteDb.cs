using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.Sqlite;
using NReco.Data;

namespace PivotDataService.CustomConnectorWebApi.Services {
	public class SQLiteDb {

		public IDbConnection DbConnection { get; private set; } 

		public IDbFactory DbFactory { get; private set; }

		public IDbCommandBuilder DbCommandBuilder { get; private set; }

		public DbDataAdapter DataAdapter { get; private set; }

		public SQLiteDb(string sqliteDbFileName) {
			DbFactory = new DbFactory(SqliteFactory.Instance);
			DbCommandBuilder = new DbCommandBuilder(DbFactory);

			var conn = DbFactory.CreateConnection();
			conn.ConnectionString = String.Format("Data Source={0}", sqliteDbFileName);
			DbConnection = conn;

			DataAdapter = new DbDataAdapter(DbConnection, DbCommandBuilder);
		}

		public void CreateSampleData() {
			Execute(@"CREATE TABLE [test]  (
					[id] INTEGER PRIMARY KEY,
					[name] TEXT,
					[category] TEXT,
					[country] TEXT,
					[date_year] INTEGER,
					[date_month] INTEGER,
					[date_day] INTEGER,
					[age] INTEGER
				)");
			var rs = new RecordSet(new RecordSet.Column[] {
				new RecordSet.Column("id", typeof(int)),
				new RecordSet.Column("name", typeof(string)),
				new RecordSet.Column("category", typeof(string)),
				new RecordSet.Column("country", typeof(string)),
				new RecordSet.Column("date_year", typeof(int)),
				new RecordSet.Column("date_month", typeof(int)),
				new RecordSet.Column("date_day", typeof(int)),
				new RecordSet.Column("age", typeof(int)),
			});
			rs.PrimaryKey = new RecordSet.Column[] { rs.Columns["id"] };
			for (int i = 0; i < 100000; i++) {
				var r = rs.Add();
				var id = i + 1;
				r["id"] = id;
				r["name"] = "Name #" + id.ToString();
				r["category"] = "Category_" + ((id % 5) + 1).ToString();
				r["country"] = getCountry(id);
				var dt = DateTime.Now.AddDays(-((id % 1000) + 1));
				r["date_year"] = dt.Year;
				r["date_month"] = dt.Month;
				r["date_day"] = dt.Day;
				r["age"] = 18 + (i % 40);
			}
			try {
				DbConnection.Open();
				DataAdapter.Transaction = DbConnection.BeginTransaction();
				DataAdapter.Update("test", rs);
				DataAdapter.Transaction.Commit();
			} catch (Exception ex) {
				if (DataAdapter.Transaction!= null)
					DataAdapter.Transaction.Rollback();
			} finally {
				DbConnection.Close();
				DataAdapter.Transaction = null;
			}

			string getCountry(int id) {
				if (id % 11 == 0)
					return "Japan";
				if (id % 7 == 0)
					return "Canada";
				if (id % 5 == 0)
					return "Ukraine";
				return "USA";
			}

		}

		void Execute(string sql) {
			var cmd = new SqliteCommand(sql);
			cmd.Connection = (SqliteConnection)DbConnection;
			DbConnection.Open();
			try { 
				cmd.ExecuteNonQuery(); 
			} finally {
				DbConnection.Close();
			}
		}

	}
}
