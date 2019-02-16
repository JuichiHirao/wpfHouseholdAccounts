using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Diagnostics;
using NLog;

namespace wpfHouseholdAccounts
{
    public class MoneyInputRegist
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        List<MoneyInputData> listInputData = null;
        DbConnection dbcon = null;
        Account account = null;

        public MoneyInputRegist(List<MoneyInputData> myList, Account myAccount, DbConnection myDbCon)
        {
            listInputData = myList;
            account = myAccount;
            dbcon = myDbCon;
        }
        public void Cancel(DateTime myDate)
        {
            // データベース：トランザクションを開始
            dbcon.BeginTransaction("MONEYINPUTCANCELBEGIN");

            try
            {
                // 金銭帳から対象のデータを取得
                List<MakeupDetailData> listData = MoneyInput.GetDataFromDate(myDate, dbcon);

                // 現金の指定日付の行を削除
                Cash.DbDeleteFromDate(myDate, dbcon);

                // 預金の指定日付の行を削除
                BankAccount.DbDeleteFromDate(myDate, dbcon);

                // 金銭帳の指定日付の行を削除
                MoneyInput.DbDeleteFromDate(myDate, dbcon);
                /*
                foreach(MakeupDetailData data in listData)
                {
                    string DebitKind = account.getAccountKind(data.DebitCode);

                    // 科目種別が「負債：借入」の場合
                    if (DebitKind == Account.KIND_DEPT_LOAN)
                    {
                        Loan loan = Loan.Cancel(data, account, dbcon);
                    }
                } */
            }
            catch(Exception ex)
            {
                _logger.ErrorException("Exception発生 ", ex);
                // データベースの更新をロールバックする
                dbcon.RollbackTransaction();
                throw ex;
            }

            // データベースの更新をコミットする
            dbcon.CommitTransaction();

            return;
        }
        public void Execute(DateTime myRegistDate, MoneyNowData myCashInfo, MoneyNowData myCashExpenseCompanyInfo)
        {
            // データベース：トランザクションを開始
            dbcon.BeginTransaction("MONEYINPUTBEGIN");

            // 支払確定から支払日に一致するＲｏｗを削除する

            // 現金の合計額を算出、格納
            long DebitCashTotal = 0;
            long CreditCashTotal = 0;

            // OBJ現金の生成
            Cash cash = new Cash();
            // OBJ預金の生成
            BankAccount bankdata = new BankAccount(myRegistDate);
            // OBJ立替の生成
            Advance advance = new Advance();
            // OBJ予算の生成
            Budget budget = new Budget(myRegistDate);

            try
            {
                foreach (MoneyInputData inputdata in listInputData)
                {
                    inputdata.Date = myRegistDate;

                    string DebitKind = account.getAccountKind(inputdata.DebitCode);
                    string CreditKind = account.getAccountKind(inputdata.CreditCode);

                    // 科目種別が「現金」の場合
                    if (DebitKind == Account.KIND_ASSETS_CASH
                        || CreditKind == Account.KIND_ASSETS_CASH)
                    {
                        // 借方が現金の場合
                        if (DebitKind == Account.KIND_ASSETS_CASH)
                            DebitCashTotal += inputdata.Amount;
                        // 貸方が現金の場合
                        if (CreditKind == Account.KIND_ASSETS_CASH)
                            CreditCashTotal += inputdata.Amount;
                    }

                    // 科目種別が「予算」の場合
                    if (DebitKind == Account.KIND_ASSETS_BUDGET
                        || CreditKind == Account.KIND_ASSETS_BUDGET)
                    {
                        // 借方が予算の場合
                        if (DebitKind == Account.KIND_ASSETS_BUDGET)
                            budget.Compilation(inputdata.DebitCode, inputdata.CreditCode, inputdata.Amount);
                        // 貸方が予算の場合
                        if (DebitKind == Account.KIND_ASSETS_BUDGET)
                            budget.Appropriation(inputdata.CreditCode, inputdata.DebitCode, inputdata.Amount);
                    }

                    // 科目種別が「負債：借入」の場合
                    if (DebitKind == Account.KIND_DEPT_LOAN
                        || CreditKind == Account.KIND_DEPT_LOAN)
                    {
                        Loan loan = new Loan(inputdata, account, dbcon);
                    }

                    // 科目種別が「負債：未払」の場合
                    if (DebitKind == Account.KIND_DEPT_APPEAR
                        || CreditKind == Account.KIND_DEPT_APPEAR)
                    {
                        Arrear arrear = new Arrear();

                        arrear.Adjustment(inputdata, dbcon);
                    }

                    // 科目種別が「負債：未払xxx」の場合
                    if (DebitKind == Account.KIND_PAYMENT_ARREAR
                        || CreditKind == Account.KIND_PAYMENT_ARREAR)
                    {
                        Arrear arrear = new Arrear();

                        arrear.Adjustment(inputdata, dbcon);
                    }

                    // 科目種別が「負債：未払」の場合
                    if (DebitKind == Account.KIND_ASSETS_COMPANY_ARREAR)
                    {
                        Arrears arrears = new Arrears();

                        arrears.Reception(inputdata, dbcon);

                        // COMPANY_ARREARS_ASSETSから対応する資産コードを取得する
                        string code = arrears.GetAsset(inputdata.DebitCode, null);
                        string kind = account.getAccountKind(code);

                        if (kind.Equals(Account.KIND_ASSETS_CASH))
                            DebitCashTotal += inputdata.Amount;
                        else if (kind.Equals(Account.KIND_ASSETS_DEPOSIT))
                            bankdata.Deposit(code, inputdata.Amount);
                    }

                    // 科目種別が「資産：預金、貯蓄」の場合
                    if ((DebitKind == Account.KIND_ASSETS_DEPOSIT || DebitKind == Account.KIND_ASSETS_SAVINGS)
                        || (CreditKind == Account.KIND_ASSETS_DEPOSIT || CreditKind == Account.KIND_ASSETS_SAVINGS))
                    {
                        // 借方が預金の場合
                        if (DebitKind == Account.KIND_ASSETS_DEPOSIT || DebitKind == Account.KIND_ASSETS_SAVINGS)
                            bankdata.Deposit(inputdata.DebitCode, inputdata.Amount);
                        // 貸方が預金の場合
                        if (CreditKind == Account.KIND_ASSETS_DEPOSIT || CreditKind == Account.KIND_ASSETS_SAVINGS)
                            bankdata.Draw(inputdata.CreditCode, inputdata.Amount);
                    }
                    // 科目種別が「資産：立替」の場合
                    if (DebitKind == Account.KIND_ASSETS_ADVANCE
                        || CreditKind == Account.KIND_ASSETS_ADVANCE)
                    {
                        // 借方が立替の場合（立替明細へ登録）
                        if (DebitKind == Account.KIND_ASSETS_ADVANCE)
                            advance.DatabaseAdvanceDetailInsert(inputdata, dbcon);
                        // 貸方が立替の場合（立替明細から削除、明細履歴へ登録）
                        if (CreditKind == Account.KIND_ASSETS_ADVANCE)
                        {
                            // 立替金はCOL金銭帳入力で一度だけ処理を行う

                            // 一部受取の場合は新たにＲｏｗを作成する明細履歴には一部の金額で登録する
                            // この時の摘要は前のものを引き継ぎ日付を含める（ex. 「2005-03-19 XXXXXの残額」）
                            long myAdvanceSummary = MoneyInput.CalcAccountTotal(listInputData, MoneyInput.CACL_KUBUN_CREDIT, inputdata.CreditCode);

                            // 精算時の合計が一致している事のチェックを行う
                            if (advance.DatabaseKeepAccurateCheck(inputdata, myAdvanceSummary, dbcon) == true)
                            {
                                advance.DatabaseAdvanceHistoryInsert(inputdata.Date, dbcon);
                                advance.DatabaseAdvanceDetailDelete(inputdata.Date, dbcon);
                            }
                        }
                    }
                    // 科目種別が「会社用預金の場合」
                    if (DebitKind == Account.KIND_COMPANY_EXPENSE_BANK
                        || CreditKind == Account.KIND_COMPANY_EXPENSE_BANK)
                    {
                        // 借方が預金の場合
                        if (DebitKind == Account.KIND_COMPANY_EXPENSE_BANK)
                            bankdata.Deposit(Account.CODE_THETAINC_BANK, inputdata.Amount);
                        // 貸方が預金の場合
                        if (CreditKind == Account.KIND_COMPANY_EXPENSE_BANK)
                            bankdata.Draw(Account.CODE_THETAINC_BANK, inputdata.Amount);
                    }

                    // 金銭帳へ登録
                    MoneyInput.InsertDbData(inputdata, dbcon);
                }

                // OBJ預金のデータベースへの反映（１日分を纏めてＤＢへ反映）
                bankdata.DatabaseRefrect(dbcon);

                cash.RegistDataCheck(myRegistDate, DebitCashTotal, CreditCashTotal);
                cash.RegistCompanyDataCheck(myRegistDate, myCashExpenseCompanyInfo.DebitAmount, myCashExpenseCompanyInfo.CreditAmount);

                // OBJ現金のデータベースへの反映（１日分を纏めてＤＢへ反映）
                cash.DatabaseRefrect(dbcon);
                // OBJ予算のデータベースへの反映（１日分を纏めてＤＢへ反映）
                budget.DatabaseRefrect(dbcon);
            }
            catch (SqlException errsql)
            {
                _logger.Error(errsql, "SqlException発生 ");
                // データベースの更新をロールバックする
                dbcon.RollbackTransaction();
                throw errsql;
            }
            catch (BussinessException errbsn)
            {
                _logger.Error(errbsn, "BussinessException発生 ");
                // データベースの更新をロールバックする
                dbcon.RollbackTransaction();
                throw errbsn;
            }
            catch (Exception err)
            {
                _logger.Error(err, "Exception発生");
                // データベースの更新をロールバックする
                dbcon.RollbackTransaction();
                throw err;
            }
            // データベースの更新をコミットする
            dbcon.CommitTransaction();

            return;
        }

        /// <summary>
        /// 後日確認入力の会社未払仕訳での登録用
        /// </summary>
        /// <param name="myRegistDate"></param>
        /// <param name="myCashInfo"></param>
        /// <param name="myCashExpenseCompanyInfo"></param>
        public static void Execute(MoneyInputData myInputData, Account myAccount, DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            string DebitKind = myAccount.getAccountKind(myInputData.DebitCode);
            string CreditKind = myAccount.getAccountKind(myInputData.CreditCode);

            if (DebitKind != Account.KIND_ASSETS_COMPANY_ARREAR
                && CreditKind != Account.KIND_ASSETS_COMPANY_ARREAR)
            {
                throw new BussinessException("借方 or 貸方が会社未払のコードの必要があります");
            }

            myInputData.UsedCompanyArrear = 1;

            // データベース：トランザクションを開始
            myDbCon.BeginTransaction("COMPANY_JOURNAL_BEGIN");
            try
            {
                Arrears arrears = new Arrears();
                arrears.ReceptionCompanyJournal(myInputData, myDbCon);
            }
            catch (SqlException errsql)
            {
                _logger.Error(errsql, "SqlException発生 ");
                // データベースの更新をロールバックする
                myDbCon.RollbackTransaction();
                throw errsql;
            }
            catch (BussinessException errbsn)
            {
                _logger.Error(errbsn, "BussinessException発生 ");
                // データベースの更新をロールバックする
                myDbCon.RollbackTransaction();
                throw errbsn;
            }
            catch (Exception err)
            {
                _logger.Error(err, "Exception発生");
                // データベースの更新をロールバックする
                myDbCon.RollbackTransaction();
                throw err;
            }
            // データベースの更新をコミットする
            myDbCon.CommitTransaction();

            return;
        }

        /// <summary>
        /// 後日確認入力の会社仕訳の登録用
        /// </summary>
        /// <param name="myInputData"></param>
        /// <param name="myAccount"></param>
        /// <param name="myDbCon"></param>
        public static void ExecuteJournalOnly(MoneyInputData myInputData, Account myAccount, DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            // データベース：トランザクションを開始
            myDbCon.BeginTransaction("COMPANY_JOURNAL_BEGIN");

            try
            {
                MoneyInput.InsertDbData(myInputData, myDbCon);
            }
            catch (SqlException sqlex)
            {
                _logger.Error(sqlex);
                Debug.Write(sqlex);
                if (!myDbCon.isTransaction())
                    myDbCon.RollbackTransaction();
                throw new BussinessException("SqlException発生 Arrears.Resception " + sqlex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                Debug.Write(ex);
                if (!myDbCon.isTransaction())
                    myDbCon.RollbackTransaction();
                throw new BussinessException("Exception発生 Arrears.Resception " + ex.Message);
            }

            if (!myDbCon.isTransaction())
                myDbCon.CommitTransaction();
            // データベースの更新をコミットする
            myDbCon.CommitTransaction();

            return;
        }

    }
}
