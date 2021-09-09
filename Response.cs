using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFS.AzureintegrationWorkOrder.DTO
{
    class Response
    {
		public string firstname { get; set; }
		public string lastname { get; set; }
		public string companyname { get; set; }
		public string email { get; set; }
		public string car { get; set; }
		public string name { get; set; }
		public string workorderline1 { get; set; }
		public string workorderline2 { get; set; }
		public string workorderline3 { get; set; }
		public string workorderlinepart1 { get; set; }
		public string workorderlinepart2 { get; set; }
		public string workorderlinepart3 { get; set; }
		public string service1 { get; set; }
		public string service2 { get; set; }
		public string service3 { get; set; }
		public decimal labour1 { get; set; }
		public decimal labour2 { get; set; }
		public decimal labour3 { get; set; }
		public string part1 { get; set; }
		public string part2 { get; set; }
		public string part3 { get; set; }

		//public decimal labourCost1 { get; set; }
		//public decimal labourCost2 { get; set; }
		//public decimal labourCost3 { get; set; }
	}
}
