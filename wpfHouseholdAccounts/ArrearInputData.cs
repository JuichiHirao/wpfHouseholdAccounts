using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace wpfHouseholdAccounts
{
    public class ArrearInputData
    {
        public const int SUMMARY_LENGTH = 255;
        public DateTime Date;			// 年月日
        public string ArrearCode;		// 未払コード
        public string DebitCode;		// 借方
        public long Amount;			// 金額
        public string Summary;		// 摘要
        public string Kind;
        public DateTime PaymentDate;    // 支払日（支払予定日）
        public string CreditKind;		// 借方科目種別
        public string DebitKind;		// 貸方科目種別

        public ArrearInputData()
        {
            DebitCode = "";		// 借方
            ArrearCode = "";	// 未払
        }
    }
}
