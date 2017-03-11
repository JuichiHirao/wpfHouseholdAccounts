using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfHouseholdAccounts.summary
{
    class SummaryParameter
    {
        public SummaryParameter()
        {
            IsSummaryCalcurate = false;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string ParentName { get; set; }

        public int Kind { get; set; }

        public string Debit { get; set; }

        public string DebitKind { get; set; }

        public string Credit { get; set; }

        public string CreditKind { get; set; }

        public string Remark { get; set; }

        public string SubDebit { get; set; }

        public string SubDebitKind { get; set; }

        public string SubCredit { get; set; }

        public string SubCreditKind { get; set; }

        public string SubRemark { get; set; }

        public int IsUsedCompanyArrear { get; set; }

        public DateTime ValidStartDate { get; set; }

        public DateTime ValidEndDate { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime UpdateDate { get; set; }

        public int SortOrder { get; set; }

        public long Total { get; set; }

        public long SubTotal { get; set; }

        public List<int> MatchId { get; set; }

        // 再帰で既に計算が終了しているかのフラグ
        public bool IsSummaryCalcurate { get; set; }
    }
}
