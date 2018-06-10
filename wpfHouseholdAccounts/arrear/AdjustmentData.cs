using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfHouseholdAccounts.arrear
{
    class AdjustmentData
    {
        public string Code { get; set; }

        public string AccountKind { get; set; }
        public long Amount { get; set; }
    }
}
