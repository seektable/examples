﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Data;
using PivotDataService.CustomConnectorWebApi.Models;

namespace PivotDataService.CustomConnectorWebApi.Controllers {

	[ApiController]
	[Route("customconnector")]
	public class CustomConnectorController : ControllerBase {

		private readonly ILogger<CustomConnectorController> _logger;

		public CustomConnectorController(ILogger<CustomConnectorController> logger) {
			_logger = logger;
		}

		[HttpPost]
		public IActionResult ExecuteQuery([FromBody]WebApiQuery query, [FromServices] Services.SQLiteDb db) {
			_logger.LogInformation("Query: " + JsonSerializer.Serialize(query));

			if (query.Columns == null || query.Columns.Length == 0)
				throw new Exception("Query should have at least one column to load.");

			// in this example raw ADO.NET interfaces are used to execute SQL generated by the custom connector's query
			// however, if your underlying data source is an SQL DB with ADO.NET provider it might be easier to use NReco.Data and its Query for the same purpose
			var dbCmd = db.DbFactory.CreateCommand();
			dbCmd.Connection = db.DbConnection;

			// depending on the report type query.Columns may contain only fields (flat-table report) or aggregate functions (pivot-table report).
			// let's generate SELECT SQL
			var columnsSqlExprs = new List<string>();
			var groupBySqlExprs = new List<string>();
			var hasAggregates = false;
			foreach (var c in query.Columns) {
				switch (c.Type) {
					case WebApiQueryColumn.TypeField:
						var fieldSql = $"[{c.Field.Name}]";
						columnsSqlExprs.Add(fieldSql);
						groupBySqlExprs.Add(columnsSqlExprs.Count.ToString());
						break;
					case WebApiQueryColumn.TypeAggregate:
						columnsSqlExprs.Add(getAggregateSqlExpr(c));
						hasAggregates = true;
						break;
				}
			}
			// generate SQL WHERE by query.FilterRelex that comes as NReco.Data relex condition (https://github.com/nreco/data/wiki/Relex)
			// query.FilterRelex is passed in 2 cases:
			// - if cube's config defines "FilterRelex" to convert report parameters into filtering conditoins
			// - if report type is 'flat-table' and user enters something in report's "Filter"
			// - if report type is 'pivot-table' and "PivotFilter.ApplyAsCondition" is enabled in the cube config and report's "Filter" can be translated into data source-level conditions
			string whereSql = null;
			if (!String.IsNullOrWhiteSpace(query.FilterRelex)) {
				// it is possible to handle filtering conditions in an alternative way:
				// query.FilterJson contains conditions in the form of MongoDB JSON filter (https://docs.mongodb.com/manual/reference/operator/query/)
				// it might be easier to parse this JSON instead of relex for non-.NET custom connector API implementations
				var relexParser = new NReco.Data.Relex.RelexParser();
				var filterCondition = relexParser.ParseCondition(query.FilterRelex);
				var sqlBuilder = db.DbFactory.CreateSqlBuilder(dbCmd, null);
				whereSql = sqlBuilder.BuildExpression(filterCondition);
			}

			var orderBySqlExprs = new List<string>();
			if (query.Sort!=null)
				foreach (var sortFld in query.Sort) {
					orderBySqlExprs.Add($"[{sortFld.Name}]" + (sortFld.Direction==System.ComponentModel.ListSortDirection.Descending?" DESC":" ASC"));
				}

			var selectSql = new StringBuilder();
			selectSql.Append("SELECT ");
			selectSql.Append(String.Join(",", columnsSqlExprs));
			selectSql.Append(" FROM [test] ");
			if (!String.IsNullOrWhiteSpace(whereSql)) {
				selectSql.Append(" WHERE ");
				selectSql.Append(whereSql);
			}
			if (hasAggregates && groupBySqlExprs.Count>0) {
				selectSql.Append(" GROUP BY ");
				// if DB supports "GROUP BY CUBE" AND cube config "HandleTotalsInResult" is true AND you want to have totals in pivot tables:
				//if (query.Columns.Any(c=>c.Type==WebApiQueryColumn.TypeAggregate && c.Aggregate.Function=="FirstValue"))
				//	selectSql.Append(" CUBE ");
				selectSql.Append(String.Join(",", groupBySqlExprs));
			}
			if (orderBySqlExprs.Count>0) {
				selectSql.Append(" ORDER BY ");
				selectSql.Append(String.Join(",", orderBySqlExprs));
			}

			if (query.Limit.HasValue) {
				selectSql.Append($" LIMIT {query.Limit} ");
			}
			if (query.Offset.HasValue) {
				selectSql.Append($" OFFSET {query.Offset} ");
			}

			dbCmd.CommandText = selectSql.ToString();

			_logger.LogInformation("Generated SQL: " + dbCmd.CommandText);

			// response should be a JSON array of rows
			// where each row is an array with values (values should go in the order that corresponds to query.Columns)
			// For example:
			// [
			//   ["a", "b", 1, 1],
			//   ["b", "b", 2, 5]
			// ]
 
			// IMPORTANT: custom connector's API can return sub-totals/grand-total explicitely if cube's config allows that ("HandleTotalsInResult" is true).
			// This makes sense when query.Columns contain a "FirstValue" aggregate function --
			// PivotDataService cannot calculate sub-totals for this kind of measures and if pivot table
			// should display totals for "FirstValue" measure these values should provided by a custom connector.
			// Rows with values for totals should have NULLs for appropriate Type=Field columns like that:
			// ["a", null, 4, 10]
			// (how totals are returned in result of GROUP BY CUBE).
			// This also means that normal rows should NOT have NULLs in columns with Type=Field. 

			return new QueryResult(dbCmd);

			string getAggregateSqlExpr(WebApiQueryColumn aggrCol) {
				if (aggrCol.Aggregate.Function!="Count") {
					// ensure at least one param value
					if (aggrCol.Aggregate.Params == null || aggrCol.Aggregate.Params.Length == 0)
						throw new Exception($"Invalid aggregate function '{aggrCol.Aggregate?.Function}': at least one parameter is required.");
				}
				switch (aggrCol.Aggregate.Function) {
					case "Count":
						return "COUNT(*)";
					case "Sum":
						return $"SUM([{aggrCol.Aggregate.Params[0]}])";
					case "Average":
						return $"AVG([{aggrCol.Aggregate.Params[0]}])";
					case "Min":
						return $"Min([{aggrCol.Aggregate.Params[0]}])";
					case "Max":
						return $"Max([{aggrCol.Aggregate.Params[0]}])";
					case "FirstValue":
						// this implementation assumes that custom SQL expression is specified in the cube configuration as a 1st parameter
						// if you don't want to allow raw SQL from the cube config, Params[0] can be a just a name (like 'CountUnique') that is resolved to SQL here
						return Convert.ToString( aggrCol.Aggregate.Params[0] );
					default:
						throw new Exception($"Unknown aggregate function '{aggrCol.Aggregate?.Function}'.");
				}
			}
		}


		public class QueryResult : IActionResult {

			IDbCommand Cmd;

			public QueryResult(IDbCommand dbCmd) {
				Cmd = dbCmd;
			}

			void WriteValue(Utf8JsonWriter jsonWr, object val) {
				switch (val) {
					case byte byteVal:
						jsonWr.WriteNumberValue(byteVal);
						break;
					case sbyte sbyteVal:
						jsonWr.WriteNumberValue(sbyteVal);
						break;
					case UInt16 uint16val:
						jsonWr.WriteNumberValue(uint16val);
						break;
					case UInt32 uint32val:
						jsonWr.WriteNumberValue(uint32val);
						break;
					case UInt64 uint64val:
						jsonWr.WriteNumberValue(uint64val);
						break;
					case Int16 int16val:
						jsonWr.WriteNumberValue(int16val);
						break;
					case Int32 int32val:
						jsonWr.WriteNumberValue(int32val);
						break;
					case Int64 int64val:
						jsonWr.WriteNumberValue(int64val);
						break;
					case decimal decVal:
						jsonWr.WriteNumberValue(decVal);
						break;
					case double doubleVal:
						jsonWr.WriteNumberValue(doubleVal);
						break;
					case Single floatVal:
						jsonWr.WriteNumberValue(floatVal);
						break;
					case String strVal:
						jsonWr.WriteStringValue(strVal);
						break;
					case bool bVal:
						jsonWr.WriteBooleanValue(bVal);
						break;
					case DateTime dtVal:
						jsonWr.WriteStringValue(dtVal.ToString("u"));
						break;
					default:
						throw new Exception($"Cannot serialize to JSON .NET type '{val.GetType()}'");
				}
			}

			public Task ExecuteResultAsync(ActionContext context) {
				var response = context.HttpContext.Response;
				response.ContentType = "application/json; charset=utf-8";

				Cmd.Connection.Open();
				try {
					using (var rdr = Cmd.ExecuteReader()) {
						using (var jsonWr = new Utf8JsonWriter(response.Body, new JsonWriterOptions() {
							Indented = false
						})) {
							jsonWr.WriteStartArray();
							var values = new object[rdr.FieldCount];
							while (rdr.Read()) {
								rdr.GetValues(values);
								jsonWr.WriteStartArray();
								for (int i = 0; i < values.Length; i++) {
									var val = values[i];
									if (val == null || DBNull.Value.Equals(val)) {
										jsonWr.WriteNullValue();
									} else {
										WriteValue(jsonWr, val);
									}
								}
								jsonWr.WriteEndArray();
							}

							jsonWr.WriteEndArray();
						};
					}
				} finally {
					Cmd.Connection.Close();
				}
				
				return Task.CompletedTask;
			}
		}




	}
}
