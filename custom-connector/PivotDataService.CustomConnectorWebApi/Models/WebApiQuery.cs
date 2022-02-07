using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PivotDataService.CustomConnectorWebApi.Models {

	/// <summary>
	/// Represents query composed by PivotDataService
	/// </summary>
	public class WebApiQuery {
		public WebApiQuery() {
		}

		/// <summary>
		/// List of columns to load. 
		/// Columns order in the JSON result should correspond to this list.  
		/// </summary>
		public WebApiQueryColumn[] Columns { get; set; }

		/// <summary>
		/// Sort criteria. Used only for flat-table reports (no aggregates in the "Columns" list). 
		/// </summary>
		public WebApiSort[] Sort { get; set; }

		/// <summary>
		/// Resolved values of parameters.
		/// </summary>
		public Dictionary<string, object> Parameters { get; set; }

		/// <summary>
		/// Filtering condition in the form of relex (https://github.com/nreco/data/wiki/Relex).
		/// </summary>
		public string FilterRelex { get; set; }

		/// <summary>
		/// Filtering condition in the form of Mongo JSON filter (https://docs.mongodb.com/manual/reference/operator/query/)
		/// </summary>
		public string FilterJson { get; set; }

		/// <summary>
		/// Offset of the 1st row to return. Used only for flat-tables.
		/// </summary>
		public int? Offset { get; set; }

		/// <summary>
		/// Max number of rows to return.
		/// </summary>
		public int? Limit { get; set; }

	}

	public class WebApiQueryColumn {

		public const string TypeField = "Field";
		public const string TypeAggregate = "Aggregate";

		/// <summary>
		/// Type of the column: 'Field' or 'Aggregate'.
		/// </summary>
		public string Type { get; set; }

		public WebApiQueryField Field { get; set; }

		public WebApiQueryAggregate Aggregate { get; set; }
	}

	public class WebApiQueryAggregate {
		// PivotDataService can use these values:
		// Count, Sum, Average, Min, Max, FirstValue
		// For Sum/Average/Min/Max field name is passed in Params[0]
		// For FirstValue Params may contain anything that is needed for the custom aggregate function. 
		public string Function { get; set; }
		public object[] Params { get; set; }
	}

	public class WebApiQueryField {
		public string Name { get; set; }
	}

	public class WebApiSort {
		public string Name { get; set; }
		public System.ComponentModel.ListSortDirection Direction { get; set; }
	}

}
