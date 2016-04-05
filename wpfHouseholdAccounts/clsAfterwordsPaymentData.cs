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
    public class AfterwordsPaymentData : INotifyPropertyChanged
    {
        int[] arrKind = { 1, 2, 3, 4, 5 };
        string[] arrLabel = { "月必須", "年必須", "不定期", "複数日付設定", "一時" };

        public const int KIND_TEMPORARY = 5;

        // 登録年月日, 借方, MST_A.科目名, 貸方, MST_B.科目名, 金額, 支払確定日, 摘要, 種別, 前回支払日
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public AfterwordsPaymentData()
        {
            DateTime dt = DateTime.Now;
            //Id = Convert.ToInt32(dt.ToString("MMddHHmmss"));
            Remark = "";
            DisplayDecisionDate = "";
            IsPropertyUpdate = false;
        }

        public bool IsPropertyUpdate { get; set; }

        public int Id { get; set; }
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
        private DateTime _RegistDate;
        public DateTime RegistDate
        {
            get
            {
                return _RegistDate;
            }
            set
            {
                DateTime inputdate = Convert.ToDateTime(value);
                if (inputdate.Year == 1)
                    DisplayDate = "";
                else
                    DisplayDate = inputdate.ToString("yyyy/MM/dd");

                _RegistDate = value;
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
                IsPropertyUpdate = true;
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
                IsPropertyUpdate = true;
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
        public int Kind { get; set; }
        private long _Amount;
        public long Amount
        { 
            get
            {
                return _Amount;
            }
            set
            {
                _Amount = value;
                IsPropertyUpdate = true;
                NotifyPropertyChanged("Amount");
            }
        }
        private string _Remark;
        public string Remark
        {
            get
            {
                return _Remark;
            }
            set
            {
                _Remark = value;
                IsPropertyUpdate = true;
                NotifyPropertyChanged("Remark");
            }
        }
        private int _Area;
        public int Area
        {
            get
            {
                return _Area;
            }
            set
            {
                _Area = value;
                IsPropertyUpdate = true;
                NotifyPropertyChanged("Area");
                NotifyPropertyChanged("AreaName");
            }
        }
        private string _AreaName;
        public string AreaName
        {
            get
            {
                if (Area == 1)
                    _AreaName = "国内";
                else if (Area == 2)
                    _AreaName = "海外";
                else
                    _AreaName = "";

                return _AreaName;
            }
            set
            {
                _AreaName = value;
                IsPropertyUpdate = true;
                NotifyPropertyChanged("AreaName");
            }
        }
        public int OrderSameDate { get; set; }
        public DateTime? LastTimePaymentDate { get; set; }

        public string _DisplayDecisionDate;
        public string DisplayDecisionDate
        {
            get
            {
                return _DisplayDecisionDate;
            }
            set
            {
                try
                {
                    if (value.Length > 0)
                    {
                        DateTime dt = Convert.ToDateTime(value);
                        _DecisionDate = dt;
                    }
                    else
                    {
                        _DecisionDate = null;
                    }
                    _DisplayDecisionDate = value;
                    IsPropertyUpdate = true;
                }
                catch(Exception)
                {
                    //dt = new DateTime(1900, 1, 1);
                }
                NotifyPropertyChanged("DisplayDecisionDate");
            }
        }
        private DateTime? _DecisionDate;
        public DateTime? DecisionDate
        {
            get
            {
                return _DecisionDate;
            }
            set
            {
                DateTime inputdate = Convert.ToDateTime(value);
                if (inputdate.Year == 1 || inputdate.Year == 1900)
                    _DisplayDecisionDate = "";
                else
                    _DisplayDecisionDate = inputdate.ToString("yyyy/MM/dd");

                _DecisionDate = value;
                NotifyPropertyChanged("DisplayDecisionDate");
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

            mySqlCommand = "INSERT INTO 後日確認払 ";
            mySqlCommand = mySqlCommand + "( 借方, 貸方, 金額, 摘要, 種別, 順番, AREA, 確定日 ) ";
            mySqlCommand = mySqlCommand + "VALUES( @借方, @貸方, @金額, @摘要, @種別, @順番, @AREA, @確定日 ) ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[8];

            sqlparams[0] = new SqlParameter("@借方", SqlDbType.VarChar);
            sqlparams[0].Value = DebitCode;
            sqlparams[1] = new SqlParameter("@貸方", SqlDbType.VarChar);
            sqlparams[1].Value = CreditCode;
            sqlparams[2] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[2].Value = Amount;
            sqlparams[3] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[3].Value = Remark;
            sqlparams[4] = new SqlParameter("@種別", SqlDbType.Int);
            sqlparams[4].Value = Kind;
            sqlparams[5] = new SqlParameter("@順番", SqlDbType.Int);
            sqlparams[5].Value = OrderSameDate;
            sqlparams[6] = new SqlParameter("@AREA", SqlDbType.Int);
            sqlparams[6].Value = Area;
            sqlparams[7] = new SqlParameter("@確定日", SqlDbType.Date);
            sqlparams[7].Value = DecisionDate;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            IsPropertyUpdate = false;

            return;
        }

        public void DbUpdate(DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "UPDATE 後日確認払 ";
            mySqlCommand = mySqlCommand + "SET 借方 = @借方, 貸方 = @貸方, 金額 = @金額, 摘要 = @摘要, 種別 = @種別, 順番 = @順番, AREA = @AREA, 前回支払日 = @前回支払日, 確定日 = @確定日 ";
            mySqlCommand = mySqlCommand + "WHERE [後日確認ＩＤ] = @ID ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());


            SqlParameter[] sqlparams = new SqlParameter[10];

            sqlparams[0] = new SqlParameter("@借方", SqlDbType.VarChar);
            sqlparams[0].Value = DebitCode;
            sqlparams[1] = new SqlParameter("@貸方", SqlDbType.VarChar);
            sqlparams[1].Value = CreditCode;
            sqlparams[2] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[2].Value = Amount;
            sqlparams[3] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[3].Value = Remark;
            sqlparams[4] = new SqlParameter("@種別", SqlDbType.Int);
            sqlparams[4].Value = Kind;
            sqlparams[5] = new SqlParameter("@順番", SqlDbType.Int);
            sqlparams[5].Value = OrderSameDate;
            sqlparams[6] = new SqlParameter("@AREA", SqlDbType.Int);
            sqlparams[6].Value = Area;
            sqlparams[7] = new SqlParameter("@前回支払日", SqlDbType.Date);
            if (LastTimePaymentDate == null)
                sqlparams[7].Value = DBNull.Value;
            else
                sqlparams[7].Value = LastTimePaymentDate;
            sqlparams[8] = new SqlParameter("@確定日", SqlDbType.Date);
            if (DecisionDate == null)
                sqlparams[8].Value = DBNull.Value;
            else
                sqlparams[8].Value = DecisionDate;
            sqlparams[9] = new SqlParameter("@ID", SqlDbType.Int);
            sqlparams[9].Value = Id;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            IsPropertyUpdate = false;

            return;
        }

        public void DbDelete(DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "DELETE FROM 後日確認払 ";
            mySqlCommand = mySqlCommand + "WHERE [後日確認ＩＤ] = @ID ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@ID", SqlDbType.Int);
            sqlparams[0].Value = Id;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            IsPropertyUpdate = false;

            return;
        }

    }

}
