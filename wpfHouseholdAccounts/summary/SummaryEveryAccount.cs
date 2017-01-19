using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace wpfHouseholdAccounts.summary
{
    class SummaryEveryAccountData
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public int Kind { get; set; }

        public long Total { get; set; }
    }
    class SummaryEveryAccount
    {
        public int DebitCount = 0;

        public int CreditCount = 0;

        public List<SummaryEveryAccountData> listData = null;

        public ICollectionView ColViewData = null;

        public SummaryEveryAccount(List<MakeupDetailData> myDetailData, Account myAccount)
        {
            listData = new List<SummaryEveryAccountData>();

            foreach(MakeupDetailData detailData in myDetailData)
            {
                bool isExistDebit = false, isExistCredit = false;
                foreach (SummaryEveryAccountData data in listData)
                {
                    if (detailData.DebitCode.Equals(data.Code))
                    {
                        data.Total += detailData.Amount;
                        isExistDebit = true;
                    }
                    if (detailData.CreditCode.Equals(data.Code))
                    {
                        data.Total += detailData.Amount;
                        isExistCredit = true;
                    }
                }

                if (!isExistDebit)
                {
                    SummaryEveryAccountData data = new SummaryEveryAccountData();

                    data.Code = detailData.DebitCode;
                    data.Name = myAccount.getName(data.Code);
                    data.Kind = 1;
                    data.Total = detailData.Amount;

                    listData.Add(data);
                    DebitCount++;
                }
                if (!isExistCredit)
                {
                    SummaryEveryAccountData data = new SummaryEveryAccountData();

                    data.Code = detailData.CreditCode;
                    data.Name = myAccount.getName(data.Code);
                    data.Kind = 2;
                    data.Total = detailData.Amount;

                    listData.Add(data);
                    CreditCount++;
                }
            }

            ColViewData = CollectionViewSource.GetDefaultView(listData);

            ColViewData.SortDescriptions.Clear();
            ColViewData.SortDescriptions.Add(new SortDescription("Kind", ListSortDirection.Ascending));
            ColViewData.SortDescriptions.Add(new SortDescription("Code", ListSortDirection.Ascending));

        }
    }
}
