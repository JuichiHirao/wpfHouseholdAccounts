using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Diagnostics;

namespace wpfHouseholdAccounts
{
    class RowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value.ToString();

            if (strValue.Equals("1")) // 変更
                return new LinearGradientBrush(Colors.LightSeaGreen, Colors.LightSeaGreen, 45);
            else if (strValue.Equals("2")) // 削除
                return new LinearGradientBrush(Colors.LightGray, Colors.LightGray, 45);

            return new LinearGradientBrush(Colors.White, Colors.White, 45);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MakeupDetailData : INotifyPropertyChanged
    {
        public MakeupDetailData()
        {
            _Operate = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get
            {
                return _IsSelected;
            }
            set
            {
                _IsSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        public Boolean IsCompany()
        {
            bool bResult = false;

            if (DebitCode.Equals("12011") && CompanyArrearsDetailId > 0)
                return false;

            if (CreditCode.IndexOf("31") == 0)
                bResult = true;
            else if (DebitCode.Equals("30402")) // 消費税仮受を収入への振替
                bResult = true;
            else if (DebitCode.Equals("13001")) // 売掛金、貸方は売上、消費税仮受
                bResult = true;
            else if (DebitCode.Equals("10101") || CreditCode.Equals("10101"))
                bResult = true;
            else if (DebitCode.Equals("10209") || CreditCode.Equals("10209"))
                bResult = true;
            else if (DebitCode.Equals("21002") || CreditCode.Equals("21002"))
                bResult = true;
            else if (CreditCode.Equals("53002"))
                bResult = true;
            else if (CreditCode.Equals("20801"))
            {
                if (Kind == 1)
                    bResult = true;
            }

            if (UsedCompanyArrear > 0)
                bResult = true;

            return bResult;
        }

        public int Id { get; set; }

        public int CompanyArrearsDetailId { get; set; }
        public int Kind { get; set; }
        public DateTime Date { get; set; }
        /// <summary>
        /// 詳細画面で内容を編集した場合に実行されるメソッド
        /// エラーの場合は元の値に戻す
        /// ※ 行の色を変えるためにOperateの値を1にする
        /// </summary>
        /// <param name="myTextboxText"></param>
        /// <returns>Formatエラーの場合は元の値、正常なら入力値</returns>
        public string SetDateFromGridTextbox(string myTextboxText)
        {
            DateTime dt;
            try
            {
                dt = Convert.ToDateTime(myTextboxText);
            }
            catch (FormatException)
            {
                return Date.ToString("yyyy/MM/dd");
            }

            if (Date != dt)
            {
                Date = dt;
                Operate = 1;
            }

            return Date.ToString("yyyy/MM/dd");
        }

        public string DebitCode { get; set; }

        public string DebitUpperCode { get; set; }
        /// <summary>
        /// 詳細画面で内容を編集した場合に実行されるメソッド
        /// ※ 行の色を変えるためにOperateの値を1にする
        /// </summary>
        /// <param name="myTextboxText"></param>
        public void SetDebitCodeFromGridTextbox(string myTextboxText)
        {
            if (!DebitCode.Equals(myTextboxText))
            {
                DebitCode = myTextboxText;
                Operate = 1;
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
        public string CreditCode { get; set; }

        public string CreditUpperCode { get; set; }

        private string _CreditName;
        /// <summary>
        /// 詳細画面で内容を編集した場合に実行されるメソッド
        /// エラーの場合は元の値に戻す
        /// ※ 行の色を変えるためにOperateの値を1にする
        /// </summary>
        /// <param name="myTextboxText"></param>
        public void SetCreditCodeFromGridTextbox(string myTextboxText)
        {
            if (!CreditCode.Equals(myTextboxText))
            {
                CreditCode = myTextboxText;
                Operate = 1;
            }
        }
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
        /// <summary>
        /// 詳細画面で内容を編集した場合に実行されるメソッド
        /// エラーの場合は元の値に戻す
        /// ※ 行の色を変えるためにOperateの値を1にする
        /// </summary>
        /// <param name="myTextboxText"></param>
        /// <returns>Formatエラーの場合は元の値、正常なら入力値</returns>
        public long SetAmountFromGridTextbox(string myTextboxText)
        {
            long amount;
            try
            {
                amount = Convert.ToInt32(myTextboxText.Replace(",", ""));
            }
            catch (FormatException)
            {
                return Amount;
            }

            if (Amount != amount)
            {
                Amount = amount;
                Operate = 1;
            }

            return Amount;
        }

        public string Remark { get; set; }
        private int _Operate;
        public int Operate
        {
            get
            {
                return _Operate;
            }
            set
            {
                _Operate = value;
                NotifyPropertyChanged("Operate");
            }
        }

        public int UsedCompanyArrear { get; set; }

        public DateTime RegistDate { get; set; }

        // 以下はCOMPANY_ARREARS_DETAILのプロパティ
        public int DataOrder
        {
            get;
            set;
        }

        private long _Balance;
        public long Balance
        {
            get
            {
                return _Balance;
            }
            set
            {
                _Balance = value;
                NotifyPropertyChanged("Balance");
            }
        }
    }
}
