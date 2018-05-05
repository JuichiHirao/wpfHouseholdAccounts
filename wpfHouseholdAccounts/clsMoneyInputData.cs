using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;

namespace wpfHouseholdAccounts
{
    public class MoneyInputValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value,
            System.Globalization.CultureInfo cultureInfo)
        {
            MoneyInputData inputdata = (value as BindingGroup).Items[0] as MoneyInputData;

            int CheckItem = 0;
            // 項目をチェックするのは各項目に入力されている場合のみ
            if (inputdata.DebitCode != null && inputdata.DebitCode.Length > 0)
                CheckItem++;
            if (inputdata.CreditCode != null && inputdata.CreditCode.Length > 0)
                CheckItem++;
            if (inputdata.Amount > 0)
                CheckItem++;
            if (inputdata.Remark != null && inputdata.Remark.Length > 0)
                CheckItem++;

            if (CheckItem >= 3)
            {
                if (inputdata.Date.Year < 2000)
                {
                    return new ValidationResult(false,
                        "日付が入力されていません");
                }
                if (inputdata.DebitName != null && inputdata.DebitName.Length <= 0)
                {
                    return new ValidationResult(false,
                        "正しい借方コードが入力されていません");
                }
                if (inputdata.CreditName != null && inputdata.CreditName.Length <= 0)
                {
                    return new ValidationResult(false,
                        "正しい貸方コードが入力されていません");
                }
                else
                {
                    return ValidationResult.ValidResult;
                }
            }
            else
            {
                return ValidationResult.ValidResult;
            }
        }
    }

    public class MoneyInputData : INotifyPropertyChanged
    {
        public MoneyInputData(AfterwordsPaymentData myImpData)
        {
            if (myImpData.DecisionDate != null)
                Date = (DateTime)myImpData.DecisionDate;

            DebitCode = myImpData.DebitCode;
            CreditCode = myImpData.CreditCode;
            Amount = myImpData.Amount;
            Remark = myImpData.Remark;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public int id { get; set; }
        private string _DisplayDate;
        public string DisplayDate
        {
            get
            {
                return _DisplayDate;
            }
            set
            {
                _DisplayDate = value;
                NotifyPropertyChanged("DisplayDate");
            }
        }
        private DateTime _Date;
        public DateTime Date
        {
            get
            {
                return _Date;
            }
            set
            {
                DateTime inputdate = Convert.ToDateTime(value);
                if (inputdate.Year == 1)
                    DisplayDate = "";
                else
                    DisplayDate = inputdate.ToString("yyyy/MM/dd");

                _Date = value;
            }
        }
        private string _DebitCode;
        public string DebitCode
        {
            get
            {
                return _DebitCode;
            }
            set
            {
                _DebitCode = value;
                NotifyPropertyChanged("DebitCode");
            }
        }
        private string _DebitName;
        public string DebitName
        {
            get
            {
                return _DebitName;
            }
            set
            {
                _DebitName = value;
                NotifyPropertyChanged("DebitName");
            }
        }
        private string _CreditCode;
        public string CreditCode
        {
            get
            {
                return _CreditCode;
            }
            set
            {
                _CreditCode = value;
                NotifyPropertyChanged("CreditCode");
            }
        }
        private string _CreditName;
        public string CreditName
        {
            get
            {
                return _CreditName;
            }
            set
            {
                _CreditName = value;
                NotifyPropertyChanged("CreditName");
            }
        }
        public long Amount { get; set; }
        public string Remark { get; set; }
        public int UsedCompanyArrear { get; set; }
        public MoneyInputData()
        {
            DateTime dt = DateTime.Now;
            id = Convert.ToInt32(dt.ToString("MMddHHmmss"));
            DebitCode = "";
            CreditCode = "";
            Amount = 0;
            Remark = "";
            UsedCompanyArrear = 0;
        }

        public MoneyInputData(ArrearInputData myArrearInputData)
        {
            Date = myArrearInputData.Date;
            CreditCode = myArrearInputData.ArrearCode;
            DebitCode = myArrearInputData.DebitCode;
            Amount = myArrearInputData.Amount;
            Remark = myArrearInputData.Summary;
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public void DbInsert(DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "INSERT INTO 金銭帳 ";
            mySqlCommand = mySqlCommand + "( 年月日, 借方, 貸方, 金額, 摘要 ) ";
            mySqlCommand = mySqlCommand + "VALUES( @年月日, @借方コード, @貸方コード, @金額, @摘要 ) ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[5];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = Date;
            sqlparams[1] = new SqlParameter("@借方コード", SqlDbType.VarChar);
            sqlparams[1].Value = DebitCode;
            sqlparams[2] = new SqlParameter("@貸方コード", SqlDbType.VarChar);
            sqlparams[2].Value = CreditCode;
            sqlparams[3] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[3].Value = Amount;
            sqlparams[4] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[4].Value = Remark;

            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

    }
}
