using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace wpfHouseholdAccounts
{
    class SearchInfo
    {
        public const int KIND_TEXT = 1;
        public const int KIND_DATE = 2;
        public const int KIND_DATERANGE = 3;

        public int Kind { get; set; }
        public string Text { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public SearchInfo()
        {
            Kind = 0;
            Text = "";
            FromDate = null;
            ToDate = null;
        }

        public SearchInfo(string myText)
        {
            Text = myText;

            Regex regDate = new Regex("([2][0]){0,1}[0-2][0-9][-/]{0,1}[0-1]{0,1}[0-9][-/]{0,1}[0-3]{0,1}[0-9]");

            try
            {
                if (regDate.IsMatch(Text))
                {
                    Kind = KIND_DATE;
                    string strDate = regDate.Match(Text).Value;
                    DateTime fromDate = Convert.ToDateTime(strDate);
                    FromDate = fromDate;

                    return;
                }
            }
            catch(FormatException ex)
            {

            }

            Regex regDateMonth = new Regex("[2][0][0-2][0-9][0-1][0-9]");
            try
            {
                if (regDateMonth.IsMatch(Text))
                {
                    Kind = KIND_DATERANGE;
                    string strmonth = regDateMonth.Match(Text).Value;
                    DateTime fromDate = new DateTime(Convert.ToInt32(strmonth.Substring(0, 4)), Convert.ToInt32(strmonth.Substring(5)), 1);
                    ToDate = fromDate.AddMonths(1).AddDays(-1);
                    FromDate = fromDate;

                    return;
                }
            }
            catch (FormatException ex)
            {

            }

            Kind = KIND_TEXT;
        }
    }
}
