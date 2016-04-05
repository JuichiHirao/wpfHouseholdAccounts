using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace wpfHouseholdAccounts
{
    public class MoneyNowData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public string Code { get; set; }                // コード
        public string Name { get; set; }                // 名前
        public string AccountKind { get; set; }
        public long NowAmount { get; set; }		        // 金額（現在金額）：家計簿金額
        // 実金額（自動）
        private long _RealAmount;
        public long RealAmount
        {
            get
            {
                return _RealAmount;
            }
            set
            {
                _RealAmount = value;
                NotifyPropertyChanged("RealAmount");
            }
        }
        // 予算
        private long _Bedget;
        public long Budget
        {
            get
            {
                return _Bedget;
            }
            set
            {
                _Bedget = value;
                NotifyPropertyChanged("Budget");
            }
        }
        // 支払予定金額（確定集計）
        private long _ScheduleAmount;
        public long ScheduleAmount
        {
            get
            {
                return _ScheduleAmount;
            }
            set
            {
                _ScheduleAmount = value;
                NotifyPropertyChanged("ScheduleAmount");
            }
        }
        // 借方合計
        private long _DebitAmount;
        public long DebitAmount
        {
            get
            {
                return _DebitAmount;
            }
            set
            {
                _DebitAmount = value;
                NotifyPropertyChanged("DebitAmount");
            }
        }

        // 貸方合計
        private long _CreditAmount;
        public long CreditAmount
        {
            get
            {
                return _CreditAmount;
            }
            set
            {
                _CreditAmount = value;
                NotifyPropertyChanged("CreditAmount");
            }
        }
        // 残高（自動）
        private long _BalanceAmount;
        public long BalanceAmount
        {
            get
            {
                return _BalanceAmount;
            }
            set
            {
                _BalanceAmount = value;
                NotifyPropertyChanged("BalanceAmount");
            }
        }
        // 所持金額
        private long _HaveCashAmount;
        public long HaveCashAmount
        {
            get
            {
                return _HaveCashAmount;
            }
            set
            {
                _HaveCashAmount = value;
                NotifyPropertyChanged("HaveCashAmount");
            }
        }
        // 基準日残高
        private long _BaseDateBalanceAmount;
        public long BaseDateBalanceAmount
        {
            get
            {
                return _BaseDateBalanceAmount;
            }
            set
            {
                _BaseDateBalanceAmount = value;
                NotifyPropertyChanged("BaseDateBalanceAmount");
            }
        }

        public MoneyNowData()
        {
            Code = "";
            Name = "";
            AccountKind = "";
            NowAmount = 0;
            RealAmount = 0;
            Budget = 0;
            ScheduleAmount = 0;
            DebitAmount = 0;
            CreditAmount = 0;
            BalanceAmount = 0;
            HaveCashAmount = 0;
            BaseDateBalanceAmount = 0;
        }
    }
}
