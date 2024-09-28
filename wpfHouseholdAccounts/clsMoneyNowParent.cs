using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace wpfHouseholdAccounts
{
    public class MoneyNowParent
    {
        public List<MoneyNowData> listMoneyNowData;
        Cash nowdataCash;

        public MoneyNowParent()
        {
            listMoneyNowData = new List<MoneyNowData>();
        }
        public void SetInfo(Account myAccount)
        {
            listMoneyNowData = new List<MoneyNowData>();

            // 現金
            nowdataCash = new Cash();
            MoneyNowData data = nowdataCash.GetNowAssetDept();
            listMoneyNowData.Add(data);
            MoneyNowData dataCompany = nowdataCash.GetNowAssetDeptCompany();
            listMoneyNowData.Add(dataCompany);
            MoneyNowData dataCompanyGoudou = nowdataCash.GetNowAssetDeptCompanyGoudou();
            listMoneyNowData.Add(dataCompanyGoudou);

            // 予算
            BudgetAccount nowdataBudget = new BudgetAccount();
            List<MoneyNowData> workalldata = nowdataBudget.GetNowInfo();

            foreach (MoneyNowData datasub in workalldata)
                listMoneyNowData.Add(datasub);

            // 預金
            BankAccount nowdataBank = new BankAccount();
            workalldata = nowdataBank.GetNowAssetDeptDetail(myAccount);

            foreach (MoneyNowData dataSub in workalldata)
                listMoneyNowData.Add(dataSub);

            // 未払金
            Arrears arrears = new Arrears();
            MoneyNowData dataArrears = arrears.GetNowAssetDept(new DbConnection());
            listMoneyNowData.Add(dataArrears);

            return;
        }

        public MoneyNowData GetCash()
        {
            foreach(MoneyNowData data in listMoneyNowData)
            {
                if (data.Code.Equals(Account.CODE_CASH))
                    return data;
            }

            return null;
        }

        public MoneyNowData GetCashExpenseCampanyKabushiki()
        {
            foreach (MoneyNowData data in listMoneyNowData)
            {
                if (data.Code.Equals(Account.CODE_CASHEXPENSE_KABUSHIKI))
                    return data;
            }

            return null;
        }
        public MoneyNowData GetCashExpenseCampanyGoudou()
        {
            foreach (MoneyNowData data in listMoneyNowData)
            {
                if (data.Code.Equals(Account.CODE_CASHEXPENSE_GOUDOU))
                    return data;
            }

            return null;
        }

        public void SummaryDebitCredit(List<MoneyInputData> myInputData, Account myAccount)
        {
            Account account = new Account();
            Arrears arrears = new Arrears();
            foreach (MoneyNowData data in listMoneyNowData)
            {
                data.DebitAmount = 0;
                data.CreditAmount = 0;
                string kindNow = myAccount.getAccountKind(data.Code);

                foreach(MoneyInputData dataInput in myInputData)
                {
                    string DebitKind = account.getAccountKind(dataInput.DebitCode);
                    string CreditKind = account.getAccountKind(dataInput.CreditCode);

                    int idx = 1;
                    if (data.Code == "10102")
                        idx++;

                    // 科目種別が「会社未払」の場合
                    if (DebitKind == Account.KIND_ASSETS_COMPANY_ARREAR)
                    {
                        // COMPANY_ARREARS_ASSETSから対応する資産コードを取得する
                        string code = arrears.GetAsset(dataInput.DebitCode, null);
                        string kind = account.getAccountKind(code);

                        if (code.Equals(data.Code))
                            data.DebitAmount += dataInput.Amount;
                        else if (code.Equals(data.Code))
                            data.DebitAmount += dataInput.Amount;

                        if (kindNow == Account.KIND_ASSETS_COMPANY_ARREAR)
                        {
                            data.CreditAmount += dataInput.Amount;
                        }
                    }

                    if (dataInput.DebitCode.Equals(data.Code))
                        data.DebitAmount += dataInput.Amount;

                    if (dataInput.CreditCode != null && dataInput.CreditCode.Equals(data.Code))
                        data.CreditAmount += dataInput.Amount;

                    if (data.Code.Equals(Account.CODE_CASHEXPENSE_KABUSHIKI))
                    {
                        if (dataInput.DebitCode.Equals("21002"))
                            data.DebitAmount += dataInput.Amount;
                        if (dataInput.CreditCode.Equals("21002"))
                            data.CreditAmount += dataInput.Amount;
                    }

                    if (data.Code.Equals(Account.CODE_CASHEXPENSE_GOUDOU))
                    {
                        if (dataInput.DebitCode.Equals("21006"))
                            data.DebitAmount += dataInput.Amount;
                        if (dataInput.CreditCode.Equals("21006"))
                            data.CreditAmount += dataInput.Amount;
                    }

                    if (data.Code.Equals(Account.CODE_CASHEXPENSE_KABUSHIKI))
                    {
                        string kind = myAccount.getAccountKind(dataInput.DebitCode);
                        if (kind.Equals(Account.KIND_COMPANY_EXPENSE))
                            data.DebitAmount += dataInput.Amount;

                        kind = myAccount.getAccountKind(dataInput.CreditCode);
                        if (kind.Equals(Account.KIND_COMPANY_EXPENSE))
                            data.CreditAmount += dataInput.Amount;
                    }

                    if (data.Code.Equals(Account.CODE_CASHEXPENSE_GOUDOU))
                    {
                        string kind = myAccount.getAccountKind(dataInput.DebitCode);
                        if (kind.Equals(Account.KIND_EXPENSE_GOUDOU))
                            data.DebitAmount += dataInput.Amount;

                        kind = myAccount.getAccountKind(dataInput.CreditCode);
                        if (kind.Equals(Account.KIND_EXPENSE_GOUDOU))
                            data.CreditAmount += dataInput.Amount;
                    }

                    if (data.Code.Equals(Account.CODE_THETAINC_DEBIT_BANK))
                    {
                        string kind = myAccount.getAccountKind(dataInput.DebitCode);
                        if (kind.Equals(Account.KIND_COMPANY_EXPENSE_BANK))
                            data.DebitAmount += dataInput.Amount;

                        kind = myAccount.getAccountKind(dataInput.CreditCode);
                        if (kind.Equals(Account.KIND_COMPANY_EXPENSE_BANK))
                            data.CreditAmount += dataInput.Amount;
                    }
                    if (data.Code.Equals(Account.CODE_THETALCC_BANK))
                    {
                        string kind = myAccount.getAccountKind(dataInput.DebitCode);
                        if (kind.Equals(Account.KIND_EXPENSE_BANK_GOUDOU))
                            data.DebitAmount += dataInput.Amount;

                        kind = myAccount.getAccountKind(dataInput.CreditCode);
                        if (kind.Equals(Account.KIND_EXPENSE_BANK_GOUDOU))
                            data.CreditAmount += dataInput.Amount;
                    }
                }
            }
        }
        public void Calculate(DateTime myNowDate, DateTime myBaseDate, List<PaymentData> myListPaymentDeci)
        {
            // 現金・預金の計算
            foreach (MoneyNowData data in listMoneyNowData)
            {
                if (data.AccountKind.Equals(Account.KIND_ASSETS_BUDGET))
                    continue;

                data.ScheduleAmount = GetDecisionBankTotal(data.Code, myListPaymentDeci, myNowDate, myBaseDate);

                // 予算で対象預金の金額の合計を取得
                BudgetAccount nowdataBudget = new BudgetAccount();
                long BudgetAmount = nowdataBudget.GetTotalAmount(data.Code);

                if (BudgetAmount > 0)
                    data.Budget = BudgetAmount;

                // 実金額 ＝ 家計簿金額 ＋ 予算
                data.RealAmount = data.NowAmount + data.Budget;

                // 残高 ＝ 現在金額 ＋ 借方合計 － 貸方合計
                data.BalanceAmount = data.NowAmount + data.DebitAmount - data.CreditAmount;

                // 実残高 ＝ 残高 ＋ 予算
                data.HaveCashAmount = data.BalanceAmount + data.Budget;

                // 基準日残高 ＝ 実残高 － 確定集計
                data.BaseDateBalanceAmount = data.HaveCashAmount - data.ScheduleAmount;
            }

            return;
        }

        /// <summary>
        /// 支払確認グリッドから一致する銀行コードの支払金額を取得する
        /// </summary>
        /// <param name="myNowData"></param>
        private long GetDecisionBankTotal(string myBankCode, List<PaymentData> myListPaymentDecision, DateTime myNowDate, DateTime myCalcBaseDate)
        {
            long Total = 0;

            foreach (PaymentData data in myListPaymentDecision)
            {
                if (myBankCode.Equals(data.CreditCode))
                {
                    // 現在日付より前は対象としない（既に支払済であるため）
                    if (DateTime.Compare(myNowDate, data.PaymentDate) > 0)
                        continue;

                    if (DateTime.Compare(myNowDate, data.PaymentDate) == 0)
                    {
                        // MoneyInput画面で取込済の場合は計算対象としない
                        if (data.IsCapture)
                            continue;
                    }

                    // 支払確定集計基準日以前の場合のみを対象
                    if (DateTime.Compare(myCalcBaseDate, data.PaymentDate) < 0)
                        continue;

                    Total += data.Amount;
                }
            }

            return Total;
        }
    }
}
