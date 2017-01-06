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

        private List<SummaryParameter> listSummaryParameter;
        public void LoadFromDatabase()
        {
            listSummaryParameter = new List<SummaryParameter>();

            DbConnection dbcon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "SELECT ID, NAME, PARENT_NAME, KIND, DEBIT, DEBIT_KIND, CREDIT, CREDIT_KIND, REMARK ";
            SelectCommand += "    ,SUB_DEBIT, SUB_DEBIT_KIND, SUB_CREDIT, SUB_CREDIT_KIND, SUB_REMARK, IS_TOTAL, VALID_START, VALID_END, SORT_ORDER ";
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
                    summaryParameter.SortOrder = DbExportCommon.GetDbInt(reader, 17);
                    summaryParameter.CreateDate = DbExportCommon.GetDbDateTime(reader, 18);
                    summaryParameter.UpdateDate = DbExportCommon.GetDbDateTime(reader, 19);

                    listSummaryParameter.Add(summaryParameter);
                    _logger.Trace("id [" + summaryParameter.Id + "]" + summaryParameter.Name);
                }
                if (listSummaryParameter != null && listSummaryParameter.Count <= 0)
                {
                    _logger.Debug("SUMMARY_PARAMETER 未設定");
                }
            }
            finally
            {
                reader.Close();
            }
        }

        public Summary(DateTime ConditionFromDate, DateTime ConditionToDate, List<MakeupDetailData> listInputDataDetail)
        {
            LoadFromDatabase();

            ConditionFromDate = new DateTime(2016, 11, 30);
            ConditionToDate = new DateTime(2016, 12, 27);

            Account account = new Account();

            foreach(MakeupDetailData data in listInputDataDetail)
            {
                if ((data.Date.CompareTo(ConditionFromDate) >= 0
                    && data.Date.CompareTo(ConditionToDate) <= 0)
                    || (data.RegistDate.CompareTo(ConditionFromDate) >= 0
                    && data.RegistDate.CompareTo(ConditionToDate) <= 0))
                {
                    foreach(SummaryParameter summaryParameter in listSummaryParameter)
                    {
                        if (!IsParameterValid(summaryParameter))
                            continue;

                        if (MatchParameter(summaryParameter.Debit, data.DebitCode) == PARAM_NOT_MATCH)
                            continue;

                        if (MatchParameter(summaryParameter.DebitKind, account.getAccountKind(data.DebitCode)) == PARAM_NOT_MATCH)
                            continue;

                        if (MatchParameter(summaryParameter.Credit, data.CreditCode) == PARAM_NOT_MATCH)
                            continue;

                        if (MatchParameter(summaryParameter.CreditKind, account.getAccountKind(data.CreditCode)) == PARAM_NOT_MATCH)
                            continue;

                        summaryParameter.Total = summaryParameter.Total + data.Amount;
                    }
                }
            }
            foreach (SummaryParameter summaryParameter in listSummaryParameter)
            {
                if (summaryParameter.Total > 0)
                    _logger.Debug(summaryParameter.Name + " " + summaryParameter.Total);
                else
                {
                    long total = 0;
                    foreach(SummaryParameter subSummaryParameter in listSummaryParameter)
                    {
                        if (summaryParameter.Name.Equals(subSummaryParameter.ParentName))
                            total += subSummaryParameter.Total;
                    }

                    if (total > 0)
                        _logger.Debug(summaryParameter.Name + " " + total);
                }
            }
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
                if (param.IndexOf("*") >= 0 || param.IndexOf("?") >= 0)
                {
                    isMatch = Regex.IsMatch(data, WildCardToRegular(param));
                }
                else if (param.IndexOf("!") == 0)
                {
                    if (param.Substring(1) != data)
                    {
                        notMatchCount++;
                        continue;
                    }
                    //summaryParameter.Total = summaryParameter.Total + data.Amount;
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
