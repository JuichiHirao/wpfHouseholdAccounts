using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using wpfHouseholdAccounts.summary;

namespace wpfHouseholdAccounts
{
    class Summary
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public  List<SummaryParameter> listSummaryParameter;

        public void LoadFromDatabase()
        {
            listSummaryParameter = new List<SummaryParameter>();

            DbConnection dbcon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "SELECT ID, NAME, PARENT_NAME, KIND, DEBIT, DEBIT_KIND, CREDIT, CREDIT_KIND, REMARK ";
            SelectCommand += "    ,SUB_DEBIT, SUB_DEBIT_KIND, SUB_CREDIT, SUB_CREDIT_KIND, SUB_REMARK ";
            SelectCommand += "    ,IS_USED_COMPANY_ARREAR, VALID_START, VALID_END, SORT_ORDER ";
            SelectCommand += "    ,CREATE_DATE, UPDATE_DATE ";
            SelectCommand += "  FROM SUMMARY_PARAMETER ";
            SelectCommand += "  ORDER BY SORT_ORDER ASC ";

            dbcon.openConnection();

            SqlDataReader reader = null;

            SummaryParameter summaryParameter = null;
            try
            {
                SqlCommand cmd = new SqlCommand(SelectCommand, dbcon.getSqlConnection());

                SqlParameter[] sqlparams = new SqlParameter[1];

                //sqlparams[0] = new SqlParameter("@JOURNAL_ID", SqlDbType.Int);
                //sqlparams[0].Value = 0;

                //dbcon.SetParameter(sqlparams);

                reader = dbcon.GetExecuteReader(SelectCommand);

                if (reader.IsClosed)
                {
                    _logger.Info("reader.IsClosed");
                    throw new Exception("COMPANY_ARREARS_DETAILの残高の取得でreaderがクローズされています");
                }

                while (reader.Read())
                {
                    summaryParameter = new SummaryParameter();

                    summaryParameter.Id = DbExportCommon.GetDbInt(reader, 0);
                    summaryParameter.Name = DbExportCommon.GetDbString(reader, 1);
                    summaryParameter.ParentName = DbExportCommon.GetDbString(reader, 2);
                    summaryParameter.Kind = DbExportCommon.GetDbInt(reader, 3);
                    summaryParameter.Debit = DbExportCommon.GetDbString(reader, 4);
                    summaryParameter.DebitKind = DbExportCommon.GetDbString(reader, 5);
                    summaryParameter.Credit = DbExportCommon.GetDbString(reader, 6);
                    summaryParameter.CreditKind = DbExportCommon.GetDbString(reader, 7);
                    summaryParameter.Remark = DbExportCommon.GetDbString(reader, 8);
                    summaryParameter.SubDebit = DbExportCommon.GetDbString(reader, 9);
                    summaryParameter.SubDebitKind = DbExportCommon.GetDbString(reader, 10);
                    summaryParameter.SubCredit = DbExportCommon.GetDbString(reader, 11);
                    summaryParameter.SubCreditKind = DbExportCommon.GetDbString(reader, 12);
                    summaryParameter.SubRemark = DbExportCommon.GetDbString(reader, 13);
                    summaryParameter.IsUsedCompanyArrear = DbExportCommon.GetDbInt(reader, 14);
                    summaryParameter.SortOrder = DbExportCommon.GetDbInt(reader, 17);
                    summaryParameter.CreateDate = DbExportCommon.GetDbDateTime(reader, 18);
                    summaryParameter.UpdateDate = DbExportCommon.GetDbDateTime(reader, 19);

                    listSummaryParameter.Add(summaryParameter);
                    //_logger.Trace("id [" + summaryParameter.Id + "]" + summaryParameter.Name);
                }
                if (listSummaryParameter != null && listSummaryParameter.Count <= 0)
                {
                    _logger.Error("SUMMARY_PARAMETER 未設定");
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                reader.Close();
            }
        }

        public Summary(DateTime ConditionFromDate, DateTime ConditionToDate, List<MakeupDetailData> listInputDataDetail)
        {
            LoadFromDatabase();

            Account account = new Account();

            foreach(MakeupDetailData data in listInputDataDetail)
            {
                if (data.Kind == 1
                    &&(data.Date.CompareTo(ConditionFromDate) >= 0 && data.Date.CompareTo(ConditionToDate) <= 0))
                {
                    foreach (SummaryParameter summaryParameter in listSummaryParameter)
                    {
                        if (!IsParameterValid(summaryParameter))
                            continue;

                        if (data.Kind != 1)
                            continue;

                        if (MatchParameter(summaryParameter.Debit, data.DebitCode) == PARAM_NOT_MATCH)
                            continue;

                        if (MatchParameter(summaryParameter.DebitKind, account.getAccountKind(data.DebitCode)) == PARAM_NOT_MATCH)
                            continue;

                        if (MatchParameter(summaryParameter.Credit, data.CreditCode) == PARAM_NOT_MATCH)
                            continue;

                        if (MatchParameter(summaryParameter.CreditKind, account.getAccountKind(data.CreditCode)) == PARAM_NOT_MATCH)
                            continue;

                        if ((data.UsedCompanyArrear == 1 && data.UsedCompanyArrear == summaryParameter.IsUsedCompanyArrear)
                            || (data.UsedCompanyArrear != 1 && summaryParameter.IsUsedCompanyArrear != 1))
                        {
                            summaryParameter.Total = summaryParameter.Total + data.Amount;
                            if (summaryParameter.MatchId == null)
                                summaryParameter.MatchId = new List<int>();
                            summaryParameter.MatchId.Add(data.Id);
                            //_logger.Debug(summaryParameter.Debit + " id [" + data.Id + "]" + data.DebitCode + " " + data.CreditCode + " " + data.Amount);

                            if (summaryParameter.IsUsedCompanyArrear == 1)
                                _logger.Debug("1");
                        }
                        else
                            continue;

                        if (IsSubParameterValid(summaryParameter))
                        {
                            if (MatchParameter(summaryParameter.SubDebit, data.DebitCode) == PARAM_NOT_MATCH)
                                continue;

                            if (MatchParameter(summaryParameter.SubDebitKind, account.getAccountKind(data.DebitCode)) == PARAM_NOT_MATCH)
                                continue;

                            if (MatchParameter(summaryParameter.SubCredit, data.CreditCode) == PARAM_NOT_MATCH)
                                continue;

                            if (MatchParameter(summaryParameter.SubCreditKind, account.getAccountKind(data.CreditCode)) == PARAM_NOT_MATCH)
                                continue;

                            summaryParameter.SubTotal = summaryParameter.SubTotal + data.Amount;
                        }
                    }
                }
            }

            List<ParentTotal> listParentTotal = new List<ParentTotal>();

            foreach (SummaryParameter data in listSummaryParameter)
            {
                if (String.IsNullOrEmpty(data.ParentName))
                    continue;

                List<SummaryParameter> matchListParam = listSummaryParameter.FindAll(x => (x.Name == data.ParentName));
                SummaryParameter findSummaryParameter = null;
                if (matchListParam != null && matchListParam.Count > 1)
                {
                    foreach (SummaryParameter param in matchListParam)
                    {
                        if (param.SortOrder < data.SortOrder)
                        {
                            if (findSummaryParameter != null)
                            {
                                if (Math.Abs(data.SortOrder - findSummaryParameter.SortOrder) > Math.Abs(data.SortOrder - param.SortOrder))
                                    findSummaryParameter = param;
                            }
                            else
                                findSummaryParameter = param;
                        }
                    }
                }
                else
                    findSummaryParameter = matchListParam[0];

                if (findSummaryParameter == null)
                {
                    _logger.Debug("対象のSummaryParameterが見つかりません [" + data.ParentName + "]");
                    continue;
                }

                bool isMatch = false;
                int rangeSortOrder = 0;
                foreach (SummaryParameter subData in listSummaryParameter)
                {
                    if (isMatch)
                    {
                        if (subData.Kind <= findSummaryParameter.Kind)
                            break;
                        rangeSortOrder = subData.SortOrder;
                    }

                    if (subData.SortOrder == findSummaryParameter.SortOrder)
                        isMatch = true;
                }

                Calcurate(findSummaryParameter, rangeSortOrder);
            }
        }

        public void Calcurate(SummaryParameter myParam, int myRangeSortOrder)
        {
            if (myParam.IsSummaryCalcurate)
                return;

            List<SummaryParameter> matchListParam = listSummaryParameter.FindAll(x => (x.ParentName == myParam.Name));

            foreach(SummaryParameter data in matchListParam)
            {
                if (data.SortOrder < myParam.SortOrder || data.SortOrder > myRangeSortOrder)
                    continue;

                //List<SummaryParameter> matchChildData = listSummaryParameter.FindAll(x => (x.ParentName == data.Name && x.SortOrder % 10 == data.SortOrder % 10));
                List<SummaryParameter> matchChildData = listSummaryParameter.FindAll(x => (x.ParentName == data.Name));
                if (matchChildData == null || matchChildData.Count <= 0)
                    myParam.Total += data.Total;
                else
                {
                    Calcurate(data, myRangeSortOrder);
                    myParam.Total += data.Total;
                }
            }

            myParam.IsSummaryCalcurate = true;
        }

        class ParentTotal
        {
            public int SortOrder { get; set; }
            public string Name { get; set; }
            public long Total { get; set; }
        }

        public bool IsParameterValid(SummaryParameter mySummaryParameter)
        {
            if (mySummaryParameter == null)
                return false;

            if (String.IsNullOrEmpty(mySummaryParameter.Debit)
                && String.IsNullOrEmpty(mySummaryParameter.DebitKind)
                && String.IsNullOrEmpty(mySummaryParameter.Credit)
                && String.IsNullOrEmpty(mySummaryParameter.CreditKind))
                return false;

            return true;
        }

        public bool IsSubParameterValid(SummaryParameter mySummaryParameter)
        {
            if (mySummaryParameter == null)
                return false;

            if (String.IsNullOrEmpty(mySummaryParameter.SubDebit)
                && String.IsNullOrEmpty(mySummaryParameter.SubDebitKind)
                && String.IsNullOrEmpty(mySummaryParameter.SubCredit)
                && String.IsNullOrEmpty(mySummaryParameter.SubCreditKind))
                return false;

            return true;
        }

        private const int PARAM_NOT_VALID = 0;
        private const int PARAM_MATCH = 1;
        private const int PARAM_NOT_MATCH = 9;

        public int MatchParameter(string parameter, string data)
        {
            if (parameter == null || parameter.Length <= 0)
                return PARAM_NOT_VALID;

            string[] arrParam = parameter.Split(',');
            //string creditKind = account.getAccountKind(data.CreditCode);

            bool isMatch = false;
            int notMatchCount = 0;
            foreach (string param in arrParam)
            {
                if (param.IndexOf("!") == 0)
                {
                    if (param.IndexOf("*") >= 0 || param.IndexOf("?") >= 0)
                    {
                        if (!Regex.IsMatch(data, WildCardToRegular(param.Substring(1))))
                        {
                            notMatchCount++;
                            continue;
                        }
                    }
                    else if (param.Substring(1) != data)
                    {
                        notMatchCount++;
                        continue;
                    }
                }
                else
                {
                    if (param.IndexOf("*") >= 0 || param.IndexOf("?") >= 0)
                    {
                        isMatch = Regex.IsMatch(data, WildCardToRegular(param));
                        if (isMatch)
                            return PARAM_MATCH;
                    }
                    else
                    {
                        if (param == data)
                        {
                            isMatch = true;
                            break;
                        }
                    }
                }
            }

            if (notMatchCount > 0 && notMatchCount == arrParam.Length)
                isMatch = true;

            if (isMatch)
                return PARAM_MATCH;

            return PARAM_NOT_MATCH;
        }
        /**
         * http://stackoverflow.com/questions/30299671/matching-strings-with-wildcard
         */
        private static String WildCardToRegular(String value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
    }
}
