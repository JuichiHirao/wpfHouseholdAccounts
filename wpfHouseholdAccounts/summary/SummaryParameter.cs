using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfHouseholdAccounts.summary
{
    class SummaryParameter
    {
        public int Id { get; set; }

        public String Name { get; set; }

        public String ParentName { get; set; }

        public int Kind { get; set; }

        public String Debit { get; set; }

        public String DebitKind { get; set; }

        public String Credit { get; set; }

        public String CreditKind { get; set; }

        public String Remark { get; set; }

        public bool IsTotal { get; set; }

        public DateTime ValidStartDate { get; set; }

        public DateTime ValidEndDate { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime UpdateDate { get; set; }

        public int SortOrder { get; set; }

        public long Total { get; set; }
    }
}
