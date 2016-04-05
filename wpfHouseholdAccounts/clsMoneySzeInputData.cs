using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;

namespace wpfHouseholdAccounts
{
    public class MoneySzeInputValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value,
            System.Globalization.CultureInfo cultureInfo)
        {
            MoneySzeInputData inputdata = (value as BindingGroup).Items[0] as MoneySzeInputData;

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

    class MoneySzeInputData : INotifyPropertyChanged
    {
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

        public MoneySzeInputData()
        {
            DateTime dt = DateTime.Now;
            id = Convert.ToInt32(dt.ToString("MMddHHmmss"));
            Remark = "";
            //Date = null;
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

    }
}
