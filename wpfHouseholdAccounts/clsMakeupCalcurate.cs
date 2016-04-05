using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace wpfHouseholdAccounts
{
    class MakeupCalcurate
    {
        /// <summary>
        /// 引数のDateTime?[]が有効な値かのチェックする
        /// </summary>
        /// <param name="myArrDate"></param>
        /// <returns></returns>
        public static bool IsMakeupScopeDateValidValue(DateTime?[] myArrDate)
        {
            if (myArrDate[0] == null || myArrDate[1] == null)
                return false;

            if (myArrDate[0].Value.Year == 1900 || myArrDate[1].Value.Year == 1900)
                return false;

            return true;
        }
        public static string GetMakeupScopeDisplayDate(DateTime?[] myArrDate)
        {
            string result = "";

            if (myArrDate[0] == null
                || myArrDate[1] == null)
                return result;

            DateTime dtFrom = Convert.ToDateTime(myArrDate[0]);
            DateTime dtTo = Convert.ToDateTime(myArrDate[1]);

            result = dtFrom.ToString("yyyy/MM/dd") + "～" + dtTo.ToString("yyyy/MM/dd");

            return result;
        }
        /// <summary>
        /// 日付Listから指定された年度の日付を取得する
        ///   myListDateはORDER BY XXX DESCで昇順になっている必要がある
        /// </summary>
        /// <param name="myYear"></param>
        /// <param name="myListDate"></param>
        /// <returns></returns>
        public static DateTime?[] GetMakeScopeDateFiscalYear(int myYear, List<DateTime> myListDate)
        {
            DateTime?[] dtResult = new DateTime?[2];

            bool IsYearStart = false;

            dtResult[0] = new DateTime(1900, 1, 1);
            dtResult[1] = new DateTime(1900, 1, 1);

            DateTime beforeDt = new DateTime(1900, 1, 1);
            DateTime maxDt = myListDate[0];

            foreach (DateTime dt in myListDate)
            {
                if (IsYearStart)
                {
                    if (dt.Year == myYear && ((dt.Month >= 1 && dt.Month <= 3) || (dt.Month == 4 && (dt.Day >= 1 && dt.Day <= 10))))
                    {
                        dtResult[0] = dt;
                        break;
                    }
                }
                beforeDt = dt;

                if (dt.Year == myYear
                    || (dt.Year == myYear + 1 && ((dt.Month >= 1 && dt.Month <= 3) || (dt.Month == 4 && (dt.Day >= 1 && dt.Day <= 10)))))
                {
                    if (IsYearStart)
                        continue;

                    IsYearStart = true;

                    if (maxDt.CompareTo(dt) == 0)
                        dtResult[1] = dt.AddMonths(1);
                    else
                        dtResult[1] = dt.AddDays(-1);
                }
            }

            return dtResult;
        }

        public static DateTime?[] GetMakeScopeDateYear(int myYear, List<DateTime> myListDate)
        {
            DateTime?[] dtResult = new DateTime?[2];

            bool IsYearStart = false;

            dtResult[0] = new DateTime(1900, 1, 1);
            dtResult[1] = new DateTime(1900, 1, 1);

            DateTime beforeDt = new DateTime(1900, 1, 1);
            DateTime maxDt = myListDate[0];

            foreach (DateTime dt in myListDate)
            {
                if (IsYearStart)
                {
                    if (dt.Year != myYear)
                    {
                        dtResult[0] = dt;
                        break;
                    }
                }
                beforeDt = dt;

                if (dt.Year == myYear)
                {
                    if (IsYearStart)
                        continue;

                    IsYearStart = true;

                    if (maxDt.CompareTo(dt) == 0)
                        dtResult[1] = dt.AddMonths(1);
                    else
                        dtResult[1] = dt.AddDays(-1);
                }
            }

            return dtResult;
        }

        public static long GetUseBudget(DbConnection myDbCon, DateTime myFromDate, DateTime myToDate)
        {
            myDbCon.openConnection();
            // 予算からの生活費補充の金額を算出する
            string mySqlCommand = "";
            long myAmount = 0;

            mySqlCommand = "SELECT SUM(金額) FROM 金銭帳 ";
            mySqlCommand = mySqlCommand + "WHERE 年月日 >= @FROM年月日 AND 年月日 <= @TO年月日 ";
            mySqlCommand = mySqlCommand + "    AND 摘要 LIKE '生活費補充%'";

            SqlCommand myCommand = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

            SqlParameter pDateFrom = new SqlParameter("@FROM年月日", SqlDbType.DateTime);
            pDateFrom.Value = myFromDate;
            myCommand.Parameters.Add(pDateFrom);
            SqlParameter pDateTo = new SqlParameter("@TO年月日", SqlDbType.DateTime);
            pDateTo.Value = myToDate;
            myCommand.Parameters.Add(pDateTo);

            SqlDataReader reader = myCommand.ExecuteReader();

            if (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    reader.Close();
                    return 0;
                }

                Decimal myNoTarget = reader.GetDecimal(0);
                myAmount = Convert.ToInt64(myNoTarget);
                //myAmount = DbExportCommon.GetDbInt(reader, 0);
            }

            reader.Close();

            return myAmount;
        }

        public static long GetNoTarget(DbConnection myDbCon, DateTime myFromDate, DateTime myToDate)
        {
            // 集計対象外の金額を算出する
            string mySqlCommand = "";
            long myAmount = 0;

            mySqlCommand = "SELECT SUM(金額) FROM 集計対象外仕訳 ";
            mySqlCommand = mySqlCommand + "WHERE 年月日 >= @FROM年月日 AND 年月日 <= @TO年月日 ";

            SqlCommand myCommand = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

            SqlParameter pDateFrom = new SqlParameter("@FROM年月日", SqlDbType.DateTime);
            pDateFrom.Value = myFromDate;
            myCommand.Parameters.Add(pDateFrom);
            SqlParameter pDateTo = new SqlParameter("@TO年月日", SqlDbType.DateTime);
            pDateTo.Value = myToDate;
            myCommand.Parameters.Add(pDateTo);

            SqlDataReader reader = myCommand.ExecuteReader();

            if (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    reader.Close();
                    return 0;
                }

                Decimal myNoTarget = reader.GetDecimal(0);
                myAmount = Convert.ToInt64(myNoTarget);
                //myAmount = DbExportCommon.GetDbInt(reader, 0);
            }

            reader.Close();

            return myAmount;
        }
        /// <summary>
        /// 集計対象外仕訳の中で指定年月日に含める金額を算出（集計年月日を使用）
        /// </summary>
        /// <returns></returns>
        public static long GetNoTargetMakeup(DbConnection myDbCon, DateTime myFromDate, DateTime myToDate)
        {
            // 集計対象外の金額を算出する
            string mySqlCommand = "";
            long myAmount = 0;

            mySqlCommand = "SELECT SUM(金額) FROM 集計対象外仕訳 ";
            mySqlCommand = mySqlCommand + "WHERE 集計年月日 >= @FROM年月日 AND 集計年月日 <= @TO年月日 ";

            SqlCommand myCommand = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

            SqlParameter pDateFrom = new SqlParameter("@FROM年月日", SqlDbType.DateTime);
            pDateFrom.Value = myFromDate;
            myCommand.Parameters.Add(pDateFrom);
            SqlParameter pDateTo = new SqlParameter("@TO年月日", SqlDbType.DateTime);
            pDateTo.Value = myToDate;
            myCommand.Parameters.Add(pDateTo);

            SqlDataReader reader = myCommand.ExecuteReader();

            if (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    reader.Close();
                    return 0;
                }

                Decimal myNoTarget = reader.GetDecimal(0);
                myAmount = Convert.ToInt64(myNoTarget);
                //myAmount = DbExportCommon.GetDbInt(reader, 0);
            }

            reader.Close();

            return myAmount;
        }

    }
}
