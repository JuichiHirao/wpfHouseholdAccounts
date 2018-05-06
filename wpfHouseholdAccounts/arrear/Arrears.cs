using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows;
using wpfHouseholdAccounts.arrear;

namespace wpfHouseholdAccounts
{
    class Arrears
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public MoneyNowData GetNowAssetDept(DbConnection dbcon)
        {
            Arrear arrear = new Arrear();

            long balance = arrear.GetBalance(dbcon);

            MoneyNowData nowdata = new MoneyNowData();
            nowdata.Code = "12010";
            nowdata.Name = "会社未払金";
            nowdata.NowAmount = balance;

            return nowdata;
        }

        public List<ArrearInputData> GetArrearList(DbConnection dbcon)
        {
            List<ArrearInputData> listData = new List<ArrearInputData>();

            dbcon.openConnection();

            string sql = "SELECT 明細ＩＤ, 年月日, 未払コード, A1.科目名 AS 未払名, 借方コード, A2.科目名 AS 借方名, 金額, 摘要, 支払予定日 "
                        + "  FROM 未払明細 "
                        + "    LEFT JOIN 科目 AS A1 ON A1.科目コード = 未払明細.未払コード "
                        + "    LEFT JOIN 科目 AS A2 ON A2.科目コード = 未払明細.借方コード "
                        + "  ORDER BY 年月日 ";

            SqlDataReader reader = dbcon.GetExecuteReader(sql);

            if (reader.IsClosed)
            {
                throw new Exception("arrear.TargetAccountDataの取得でreaderがクローズされています");
            }

            while (reader.Read())
            {
                ArrearInputData data = new ArrearInputData();

                data.Id = DbExportCommon.GetDbInt(reader, 0);
                data.Date = DbExportCommon.GetDbDateTime(reader, 1);
                data.ArrearCode = DbExportCommon.GetDbString(reader, 2);
                data.ArrearName = DbExportCommon.GetDbString(reader, 3);
                data.DebitCode = DbExportCommon.GetDbString(reader, 4);
                data.DebitName = DbExportCommon.GetDbString(reader, 5);
                data.Amount = DbExportCommon.GetDbMoney(reader, 6);
                data.Summary = DbExportCommon.GetDbString(reader, 7);
                data.PaymentDate = DbExportCommon.GetDbDateTime(reader, 8);

                listData.Add(data);

                _logger.Trace("Id [" + data.Id + "]  入力 [" + data.Amount + "]");
            }

            return listData;
        }

        public void Regist(List<MakeupDetailData> myListData, DbConnection dbcon)
        {
            // データベース：トランザクションを開始
            dbcon.BeginTransaction("ARREAR_REGIST");

            Arrear arrear = new Arrear();

            int maxDataOrder = 0;
            try
            {
                // テーブル内にデータが存在しない場合はNullReferenceExceptionが発生するので、その場合は0とする
                try
                {
                    maxDataOrder = arrear.GetMaxDataOrder(dbcon);
                    maxDataOrder++;
                }
                catch (NullReferenceException ex)
                {
                    _logger.Warn("menuitemAddCompanyArrear_Click ", ex);
                }

                foreach (MakeupDetailData data in myListData)
                {
                    MakeupDetailData findData = null;

                    if (data.Kind == 5)
                    {
                        if (data.Id <= 0)
                            // 未払明細から一致データを取得
                            findData = Arrear.GetHistoryData(data, dbcon);
                        else
                            findData = data;

                        if (findData == null)
                            throw new Exception("未払明細履歴に一致する金銭帳のデータが存在しません");

                        // 未払明細を更新
                        Arrear.UpdateDbUsedCompanyHistoryArrear(findData.Id, 1, dbcon);
                    }
                    else if (data.Kind == 6)
                    {
                        if (data.Id <= 0)
                            // 未払明細から一致データを取得
                            findData = Arrear.GetData(data, dbcon);
                        else
                            findData = data;

                        if (findData == null)
                            throw new Exception("未払明細に一致する金銭帳のデータが存在しません");

                        // 未払明細を更新
                        Arrear.UpdateDbUsedCompanyArrear(findData.Id, 1, dbcon);
                    }
                    else
                    {
                        if (data.Id <= 0)
                            // カードの明細の場合は金銭帳データを取得
                            findData = MoneyInput.GetData(data, dbcon);
                        else
                            findData = data;

                        if (findData == null)
                            throw new Exception("カード明細に一致する金銭帳のデータが存在しません");

                        // 金銭帳を更新
                        MoneyInput.UpdateDbUsedCompanyArrear(findData.Id, 1, dbcon);
                    }

                    // COMPANY_ARREARS_DETAILへ登録
                    arrear.Regist(findData, maxDataOrder, dbcon);

                    maxDataOrder++;
                }
            }
            catch (SqlException sqlex)
            {
                _logger.Error(sqlex);
                Debug.Write(sqlex);
                if (!dbcon.isTransaction())
                    dbcon.RollbackTransaction();
                throw new BussinessException("SqlException発生 Arrears.Regist " + sqlex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                Debug.Write(ex);
                if (!dbcon.isTransaction())
                    dbcon.RollbackTransaction();
                throw new BussinessException("SqlException発生 Arrears.Regist " + ex.Message);
            }

            dbcon.CommitTransaction();
        }

        public void Cancel(List<MakeupDetailData> myListData, DbConnection dbcon)
        {
            // データベース：トランザクションを開始
            dbcon.BeginTransaction("ARREAR_CANCEL");

            Arrear arrear = new Arrear();

            int maxDataOrder = 0;
            try
            {
                // テーブル内にデータが存在しない場合はNullReferenceExceptionが発生するので、その場合は0とする
                try
                {
                    maxDataOrder = arrear.GetMaxDataOrder(dbcon);
                }
                catch (NullReferenceException ex)
                {
                    _logger.Warn("Cancel ", ex);
                }

                foreach (MakeupDetailData data in myListData)
                {
                    MakeupDetailData findData = null;

                    if (data.Id <= 0)
                        // カードの明細の場合は金銭帳データを取得
                        findData = MoneyInput.GetData(data, dbcon);
                    else
                        findData = data;

                    if (findData == null)
                        throw new Exception("カード明細に一致する金銭帳のデータが存在しません");

                    Arrear arrearData = Arrear.GetDataByJournalId(findData.Id, dbcon);

                    if (arrearData != null)
                    {
                        if (arrearData.DataOrder == maxDataOrder)
                        {
                            // COMPANY_ARREARS_DETAILから削除
                            arrear.Remove(arrearData.Id, dbcon);
                        }
                        else
                        {
                            // キャンセルデータは金額をマイナスにして登録
                            findData.Amount = findData.Amount * -1;

                            // COMPANY_ARREARS_DETAILへ登録
                            arrear.Regist(findData, maxDataOrder + 1, dbcon);
                        }
                    }

                    if (findData.Kind == 5)
                        // 未払明細を更新
                        Arrear.UpdateDbUsedCompanyArrear(findData.Id, 0, dbcon);
                    else
                        // 金銭帳を更新
                        MoneyInput.UpdateDbUsedCompanyArrear(findData.Id, 0, dbcon);
                }
            }
            catch (SqlException sqlex)
            {
                _logger.Error(sqlex);
                Debug.Write(sqlex);
                if (!dbcon.isTransaction())
                    dbcon.RollbackTransaction();
                throw new BussinessException("SqlException発生 Arrears.Cancel " + sqlex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                Debug.Write(ex);
                if (!dbcon.isTransaction())
                    dbcon.RollbackTransaction();
                throw new BussinessException("SqlException発生 Arrears.Cancel " + ex.Message);
            }

            dbcon.CommitTransaction();
        }

        public void ReceptionCompanyJournal(MoneyInputData myInputData, DbConnection dbcon)
        {
            // データベース：トランザクションを開始
            if (!dbcon.isTransaction())
                dbcon.BeginTransaction("ARREAR_RECEPTION");

            Arrear arrear = new Arrear();

            int maxId = dbcon.getIntSql("SELECT MAX(金銭帳ＩＤ) FROM 金銭帳");

            try
            {
                MoneyInput.InsertDbData(myInputData, dbcon);
                int InserId = dbcon.getIntSql("SELECT MAX(金銭帳ＩＤ) FROM 金銭帳");

                if (maxId < InserId)
                    myInputData.id = InserId;
                else
                    throw new BussinessException("金銭帳テーブルへ挿入したIDが不正です maxID [" + maxId + "] 挿入したID [" + InserId + "]");

                // iNSERTした金銭帳ＩＤ
                // COMPANY_ARREARS_DETAILへ登録
                arrear.Register(myInputData, -1, dbcon);
            }
            catch (SqlException sqlex)
            {
                _logger.Error(sqlex);
                Debug.Write(sqlex);
                if (!dbcon.isTransaction())
                    dbcon.RollbackTransaction();
                throw new BussinessException("SqlException発生 Arrears.Resception " + sqlex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                Debug.Write(ex);
                if (!dbcon.isTransaction())
                    dbcon.RollbackTransaction();
                throw new BussinessException("Exception発生 Arrears.Resception " + ex.Message);
            }

            if (!dbcon.isTransaction())
                dbcon.CommitTransaction();
        }

        public void Reception(MoneyInputData myInputData, DbConnection dbcon)
        {
            // データベース：トランザクションを開始
            if (!dbcon.isTransaction())
                dbcon.BeginTransaction("ARREAR_RECEPTION");

            Arrear arrear = new Arrear();

            int maxDataOrder = 0;
            try
            {
                // テーブル内にデータが存在しない場合はNullReferenceExceptionが発生するので、その場合は0とする
                try
                {
                    maxDataOrder = arrear.GetMaxDataOrder(dbcon);
                    maxDataOrder++;
                }
                catch (NullReferenceException ex)
                {
                    _logger.Warn("menuitemAddCompanyArrear_Click ", ex);
                }

                // COMPANY_ARREARS_DETAILへ登録
                arrear.Register(myInputData, maxDataOrder, dbcon);

                // 金銭帳を更新
                MoneyInput.UpdateDbUsedCompanyArrear(myInputData.id, 1, dbcon);
            }
            catch (SqlException sqlex)
            {
                _logger.Error(sqlex);
                Debug.Write(sqlex);
                if (!dbcon.isTransaction())
                    dbcon.RollbackTransaction();
                throw new BussinessException("SqlException発生 Arrears.Resception " + sqlex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                Debug.Write(ex);
                if (!dbcon.isTransaction())
                    dbcon.RollbackTransaction();
                throw new BussinessException("Exception発生 Arrears.Resception " + ex.Message);
            }

            if (!dbcon.isTransaction())
                dbcon.CommitTransaction();
        }

        public string GetAsset(string myCode, DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT ASSET_CODE ";
            SelectCommand = SelectCommand + "FROM COMPANY_ARREARS_ASSETS ";
            SelectCommand = SelectCommand + "WHERE COMPANY_ARREARS_CODE = @COMPANY_ARREARS_CODE ";

            myDbCon.openConnection();

            SqlDataReader reader = null;

            string code = "";
            try
            {
                SqlCommand cmd = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@COMPANY_ARREARS_CODE", SqlDbType.VarChar);
                sqlparams[0].Value = myCode;

                myDbCon.SetParameter(sqlparams);

                reader = myDbCon.GetExecuteReader(SelectCommand);

                List<MakeupDetailData> listDetail = new List<MakeupDetailData>();

                if (reader.IsClosed)
                {
                    _logger.Debug("reader.IsClosed");
                    throw new Exception("COMPANY_ARREARS_DETAILの残高の取得でreaderがクローズされています");
                }

                if (reader.Read())
                {
                    code = DbExportCommon.GetDbString(reader, 0);

                    _logger.Debug("myCode [" + myCode + "]  資産コード [" + code + "]");
                }
                else
                {
                    _logger.Debug("COMPANY_ARREARS_ASSETS データ無し");
                    throw new BussinessException("対応する会社未払金のコード[" + myCode + "]に一致する資産コードがCOMPANY_ARREARS_ASSETSに存在しません");
                }
                if (code.Length <= 0)
                {
                    _logger.Debug("COMPANY_ARREARS_ASSETS データ不正");
                    throw new BussinessException("対応する会社未払金のコード[" + myCode + "]に一致する資産コードが不正です 一致資産コード[" + code + "]");
                }
            }
            catch (SqlException sqlex)
            {
                _logger.Error(sqlex);
                Debug.Write(sqlex);
                throw new BussinessException("SqlException発生 Arrears.GetAsset " + sqlex.Message);
            }
            catch (Exception sqlex)
            {
                _logger.Error(sqlex);
                Debug.Write(sqlex);
                throw new BussinessException("SqlException発生 Arrears.GetAsset " + sqlex.Message);
            }
            finally
            {
                if (reader != null) reader.Close();
            }

            return code;
        }

        public List<Arrear> GetRangeDate(DateTime myDateFrom, DateTime myDateTo, DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT ID, DATA_ORDER, JOURNAL_ID, JOURNAL_DATE, DEBIT_CODE, CREDIT_CODE, AMOUNT, REMARK, BALANCE, CREATE_DATE, UPDATE_DATE ";
            SelectCommand = SelectCommand + "FROM COMPANY_ARREARS_DETAIL ";
            SelectCommand = SelectCommand + "WHERE JOURNAL_DATE >= @JournalDateFrom ";
            SelectCommand = SelectCommand + "  AND JOURNAL_DATE <= @JournalDateTo ";
            SelectCommand = SelectCommand + "ORDER BY JOURNAL_DATE ASC, DATA_ORDER ASC ";

            myDbCon.openConnection();

            SqlDataReader reader = null;

            List<Arrear> listArrear = new List<Arrear>();

            try
            {
                SqlCommand cmd = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@JournalDateFrom", SqlDbType.DateTime);
                sqlparams[0].Value = myDateFrom;

                sqlparams[1] = new SqlParameter("@JournalDateTo", SqlDbType.DateTime);
                sqlparams[1].Value = myDateTo;

                myDbCon.SetParameter(sqlparams);

                reader = myDbCon.GetExecuteReader(SelectCommand);

                if (reader.IsClosed)
                {
                    _logger.Debug("reader.IsClosed");
                    throw new Exception("COMPANY_ARREARS_DETAILの残高の取得でreaderがクローズされています");
                }

                while (reader.Read())
                {
                    Arrear arrear = arrear = new Arrear();

                    arrear.Id = DbExportCommon.GetDbInt(reader, 0);
                    arrear.DataOrder = DbExportCommon.GetDbInt(reader, 1);
                    arrear.JournalId = DbExportCommon.GetDbInt(reader, 2);
                    arrear.JournalDate = DbExportCommon.GetDbDateTime(reader, 3);
                    arrear.DebitCode = DbExportCommon.GetDbString(reader, 4);
                    arrear.CreditCode = DbExportCommon.GetDbString(reader, 5);
                    arrear.Amount = DbExportCommon.GetDbMoney(reader, 6);
                    arrear.Remark = DbExportCommon.GetDbString(reader, 7);
                    arrear.Balance = DbExportCommon.GetDbMoney(reader, 8);
                    arrear.CreateDate = DbExportCommon.GetDbDateTime(reader, 9);
                    arrear.UpdateDate = DbExportCommon.GetDbDateTime(reader, 10);

                    _logger.Debug("id [" + arrear.Id + "]  残高 [" + arrear.Balance + "]");
                    listArrear.Add(arrear);
                }
            }
            finally
            {
                reader.Close();
            }

            return listArrear;

        }

        public void Arrangent(DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT ID, DATA_ORDER, JOURNAL_ID, JOURNAL_DATE, DEBIT_CODE, CREDIT_CODE, AMOUNT, REMARK, BALANCE, CREATE_DATE, UPDATE_DATE ";
            SelectCommand = SelectCommand + "FROM COMPANY_ARREARS_DETAIL ";
            SelectCommand = SelectCommand + "ORDER BY JOURNAL_DATE ASC, DATA_ORDER ASC ";

            myDbCon.openConnection();

            SqlDataReader reader = null;

            Arrear arrear = null;
            try
            {
                SqlCommand cmd = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@JOURNAL_ID", SqlDbType.Int);
                sqlparams[0].Value = 0;

                myDbCon.SetParameter(sqlparams);

                reader = myDbCon.GetExecuteReader(SelectCommand);

                List<MakeupDetailData> listDetail = new List<MakeupDetailData>();

                if (reader.IsClosed)
                {
                    _logger.Debug("reader.IsClosed");
                    throw new Exception("COMPANY_ARREARS_DETAILの残高の取得でreaderがクローズされています");
                }

                if (reader.Read())
                {
                    arrear = new Arrear();

                    arrear.Id = DbExportCommon.GetDbInt(reader, 0);
                    arrear.DataOrder = DbExportCommon.GetDbInt(reader, 1);
                    arrear.JournalId = DbExportCommon.GetDbInt(reader, 2);
                    arrear.JournalDate = DbExportCommon.GetDbDateTime(reader, 3);
                    arrear.DebitCode = DbExportCommon.GetDbString(reader, 4);
                    arrear.CreditCode = DbExportCommon.GetDbString(reader, 5);
                    arrear.Amount = DbExportCommon.GetDbMoney(reader, 6);
                    arrear.Remark = DbExportCommon.GetDbString(reader, 7);
                    arrear.Balance = DbExportCommon.GetDbMoney(reader, 8);
                    arrear.CreateDate = DbExportCommon.GetDbDateTime(reader, 9);
                    arrear.UpdateDate = DbExportCommon.GetDbDateTime(reader, 10);

                    _logger.Debug("id [" + arrear.Id + "]  残高 [" + arrear.Balance + "]");
                }
                else
                {
                    _logger.Debug("COMPANY_ARREARS_DETAIL データ無し");
                }
            }
            finally
            {
                reader.Close();
            }
        }

        public void Register(List<ArrearInputData> myList, DbConnection myDbcon)
        {
            myDbcon.BeginTransaction("ARREARREGISTERTBEGIN");

            try
            {
                MoneyInput moneyin = new MoneyInput();
                Arrear arrear = new Arrear();

                // 各データの判別、処理
                foreach (ArrearInputData data in myList)
                {
                    MoneyInputData inputData = new MoneyInputData(data);

                    arrear.DatabaseDetailInsert(data, myDbcon);
                    MoneyInput.InsertDbData(inputData, myDbcon);
                }
            }
            catch (SqlException errsql)
            {
                _logger.Error(errsql);
                myDbcon.RollbackTransaction();
                throw errsql;
            }
            catch (BussinessException errbsn)
            {
                _logger.Error(errbsn);
                myDbcon.RollbackTransaction();
                throw errbsn;
            }

            // データベースの更新をコミットする
            myDbcon.CommitTransaction();
        }

        public List<AdjustmentData> CalcrateAdjustment(List<ArrearInputData> myTargetList, Account myAccount)
        {
            List<AdjustmentData> arrearCodeList = new List<AdjustmentData>();

            foreach (var target in myTargetList)
            {
                AdjustmentData findData = FindArrerCode(target.ArrearCode, arrearCodeList);
                if (findData == null)
                {
                    AdjustmentData data = new AdjustmentData();
                    data.AccountKind = myAccount.getAccountKind(target.ArrearCode);
                    if (data.AccountKind == null || data.AccountKind.Length <= 0)
                        throw new BussinessException("対象の未払い科目コードがマスタに存在しない");
                    if (data.AccountKind.Equals(Account.KIND_DEPT_APPEAR))
                        throw new BussinessException("過去の形式の未払科目コードが存在します");
                    data.Code = target.ArrearCode;
                    data.Amount = target.Amount;
                    arrearCodeList.Add(data);
                }
                else
                    findData.Amount = findData.Amount + target.Amount;
            }

            return arrearCodeList;
        }

        public AdjustmentData FindArrerCode(string myCode, List<AdjustmentData> myList)
        {
            if (myList.Count <= 0)
                return null;

            foreach (var data in myList)
                if (data.Code.Equals(myCode))
                    return data;

            return null;
        }

        public void Adjustment(List<AdjustmentData> myAdjustmentList, DateTime myPaymentDate, List<ArrearInputData> myTargetList, DbConnection myDbcon)
        {
            myDbcon.BeginTransaction("MONEYADJUSTMENTBEGIN");

            try
            {
                MethodPayment payment = new MethodPayment();
                Arrear arrear = new Arrear();

                foreach (ArrearInputData data in myTargetList)
                {
                    // 未払明細の更新
                    arrear.DatabaseDetailUpdate(data, myPaymentDate, myDbcon);
                }

                foreach (var adjustmentData in myAdjustmentList)
                {
                    if (adjustmentData.AccountKind == Account.KIND_PAYMENT_ARREAR)
                    {
                        // 支払確定挿入用にMoneyInputDataの生成
                        MoneyInputData indata = new MoneyInputData();

                        indata.Date = myPaymentDate;
                        indata.DebitCode = adjustmentData.Code;
                        indata.CreditCode = Account.CODE_CASH;    // 現金
                        indata.Amount = adjustmentData.Amount;

                        payment.DatabaseDecisionInsert(adjustmentData.Code, indata, myDbcon);
                    }
                }

                myDbcon.CommitTransaction();
            }
            catch (SqlException errsql)
            {
                myDbcon.RollbackTransaction();
                throw errsql;
            }
            catch (BussinessException errbsn)
            {
                myDbcon.RollbackTransaction();
                throw errbsn;
            }
        }

        public int UpdateRow(List<ArrearInputData> myTargetList, DbConnection myDbcon)
        {
            myDbcon.BeginTransaction("MONEYADJUSTMENTBEGIN");

            int cnt = 0;
            try
            {
                MethodPayment payment = new MethodPayment();
                Arrear arrear = new Arrear();

                foreach (ArrearInputData data in myTargetList)
                {
                    if (data.Operate == 1)
                    {
                        // 未払明細の更新
                        arrear.DatabaseDetailUpdate(data, data.PaymentDate, myDbcon);

                        MoneyInputData inputData = new MoneyInputData(data);
                        int updateRow = MoneyInput.UpdateDb(inputData, "金銭帳", myDbcon);

                        if (updateRow <= 0 || updateRow > 1)
                            throw new BussinessException("更新される行数が違っています " + updateRow);

                        cnt++;
                    }
                }

                myDbcon.CommitTransaction();
            }
            catch (SqlException errsql)
            {
                myDbcon.RollbackTransaction();
                throw errsql;
            }
            catch (BussinessException errbsn)
            {
                myDbcon.RollbackTransaction();
                throw errbsn;
            }

            return cnt;
        }

    }
}
