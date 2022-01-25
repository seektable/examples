using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PivotDataService.CustomConnectorWebApi.Models {
	public class WebApiQuery {
		public WebApiQuery() {
		}

		public WebApiQueryColumn[] Columns { get; set; }

		public WebApiSort[] Sort { get; set; }

		public Dictionary<string, object> Parameters { get; set; }

		public string Filter { get; set; }

		public int? Offset { get; set; }

		public int? Limit { get; set; }

	}

	public class WebApiQueryColumn {

		public const string TypeField = "Field";
		public const string TypeAggregate = "Aggregate";

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
