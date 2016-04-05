using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace wpfHouseholdAccounts
{
    class MakeupData
    {
        public MakeupData()
        {
            AccountUpperCode = "";
            AccountCode = "";
            AccountName = "";
            Amount = 0;
            FontSize = 11;
            
        }
        public string AccountUpperCode { get; set; }
        public string DisplayCodeName { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public long Amount { get; set; }

        public int FontSize { get; set; }
        public void SetRowStyle()
        {
            Regex regex = new Regex("^[3-7][0-9] ");

            if (regex.IsMatch(AccountUpperCode))
                FontSize = 14;
            else
            {
                if (AccountUpperCode.Equals("総合計"))
                    FontSize = 18;
                else if (AccountCode.Length > 0)
                    FontSize = 10;
                else if (AccountUpperCode.IndexOf("合計") >= 0)
                    FontSize = 16;
            }
            if (AccountCode.Length > 0)
            {
                DisplayCodeName = AccountCode + " " + AccountName;
                AccountUpperCode = "";
            }
            else
                DisplayCodeName = AccountName;
        }
    }
}
