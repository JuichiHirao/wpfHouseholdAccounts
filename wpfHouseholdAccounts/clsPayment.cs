using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace wpfHouseholdAccounts
{
    class Payment
    {
        public static void RegistDecisionFromAfterwordsPayment(List<AfterwordsPaymentData> mySourceData, Account myAccount, DbConnection myDbCon)
        {
            //////////////////////////////////////////////////////////////
            // 確定の場合												//
            //   貸方が資産：支払確定へ登録								//
            //   貸方が収益：支払確定へ登録								//
            //   貸方が負債：事前チェック								//
            //                 1) 確定日と前回支払日が一緒の場合はエラー//
            //   貸方が負債：TBL借入明細へ登録							//
            //               金銭帳へ登録								//
            //               後日確認払：前回支払日を確定日で更新		//
            //                           継続フラグにより削除			//
            //////////////////////////////////////////////////////////////

            MethodPayment payment = new MethodPayment();

            foreach (AfterwordsPaymentData data in mySourceData)
            {
                if (data.DisplayDecisionDate.Length <= 0)
                    continue;

                string codekind = myAccount.getAccountKind(data.CreditCode);

                MoneyInputData moneyinput = new MoneyInputData(data);

                if (myAccount.IsKindAssets(codekind)
                        || myAccount.IsKindProfit(codekind))
                {
                    // TBL支払確定へ登録する
                    payment.DatabaseDecisionInsert(Account.CODE_AFTERWORDS, moneyinput, myDbCon);
                }

                if (codekind.Equals(Account.KIND_DEPT_LOAN))
                {
                    // TBL借入明細へ登録
                    Loan loan = new Loan(moneyinput, myAccount, myDbCon);

                    // 金銭帳へ登録
                    moneyinput.DbInsert(myDbCon);
                }
            }
        }

        public static void CheckImported(DateTime myDate, List<PaymentData> myListPayment, List<MoneyInputData> myListInput)
        {
            bool IsTargetDate = false;
            foreach (PaymentData data in myListPayment)
            {
                if (myDate.CompareTo(data.PaymentDate) == 0)
                    IsTargetDate = true;
            }

            // 対象日のデータが存在しない場合はチェック不要
            if (!IsTargetDate)
                return;

            foreach (PaymentData data in myListPayment)
            {
                if (myDate.CompareTo(data.PaymentDate) != 0)
                    continue;

                bool IsImported = MoneyInput.IsImported(data, myListInput);

                if (IsImported)
                    data.IsCapture = true;
            }

            foreach (PaymentData data in myListPayment)
            {
                if (myDate.CompareTo(data.PaymentDate) != 0)
                    continue;

                if (!data.IsCapture)
                    throw new BussinessException("支払確定の中に本日取り込まれていないデータが存在します");
            }

        }
        public static List<PaymentData> GridvDecisionPaymentSetData()
        {
            DbConnection myDbCon = new DbConnection();
            SqlCommand myCommand;
            SqlDataReader reader;

            string SelectCommand = "";

            List<PaymentData> listPayment = new List<PaymentData>();
            /*
            SelectCommand = "SELECT 確定ＩＤ, 支払日, 支払確定.借入コード, A.科目名, 借方, B.科目名, 貸方, C.科目名, SUM(金額) AS 金額 ";
            SelectCommand = SelectCommand + "    FROM 支払確定 INNER JOIN 科目 AS A ";
            SelectCommand = SelectCommand + "        ON 支払確定.借入コード = A.科目コード ";
            SelectCommand = SelectCommand + "      INNER JOIN 科目 AS B ";
            SelectCommand = SelectCommand + "        ON 支払確定.借方 = B.科目コード ";
            SelectCommand = SelectCommand + "      INNER JOIN 科目 AS C ";
            SelectCommand = SelectCommand + "        ON 支払確定.貸方 = C.科目コード ";
            SelectCommand = SelectCommand + "    GROUP BY 支払日, 支払確定.借入コード, A.科目名, 借方, B.科目名, 貸方, C.科目名 ";
             */
            SelectCommand = "SELECT 確定ＩＤ, 支払日, 支払確定.借入コード, A.科目名, 借方, B.科目名, 貸方, C.科目名, 金額 ";
            SelectCommand = SelectCommand + "    FROM 支払確定 INNER JOIN 科目 AS A ";
            SelectCommand = SelectCommand + "        ON 支払確定.借入コード = A.科目コード ";
            SelectCommand = SelectCommand + "      INNER JOIN 科目 AS B ";
            SelectCommand = SelectCommand + "        ON 支払確定.借方 = B.科目コード ";
            SelectCommand = SelectCommand + "      INNER JOIN 科目 AS C ";
            SelectCommand = SelectCommand + "        ON 支払確定.貸方 = C.科目コード ";
            SelectCommand = SelectCommand + "  ORDER BY 支払日 ";
            //SelectCommand = SelectCommand + "    GROUP BY 支払日, 支払確定.借入コード, A.科目名, 借方, B.科目名, 貸方, C.科目名 ";

            try
            {
                myDbCon.openConnection();

                myCommand = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

                reader = myCommand.ExecuteReader();

                while (reader.Read())
                {
                    PaymentData data = new PaymentData();

                    data.Id = DbExportCommon.GetDbInt(reader, 0);
                    data.PaymentDate = DbExportCommon.GetDbDateTime(reader, 1);
                    data.LoanCode = DbExportCommon.GetDbString(reader, 2);
                    data.LoanName = DbExportCommon.GetDbString(reader, 3);
                    data.DebitCode = DbExportCommon.GetDbString(reader, 4);
                    data.DebitName = DbExportCommon.GetDbString(reader, 5);
                    data.CreditCode = DbExportCommon.GetDbString(reader, 6);
                    data.CreditName = DbExportCommon.GetDbString(reader, 7);
                    data.Amount = DbExportCommon.GetDbMoney(reader, 8);

                    listPayment.Add(data);
                }

                reader.Close();

                myDbCon.closeConnection();

            }
            catch (SqlException errsql)
            {
                String strMessage = "";

                strMessage = errsql.Message;
                strMessage = "データベースでエラーが発生しました\n" + strMessage;

                throw errsql;
            }

            return listPayment;
        }

    }
}
