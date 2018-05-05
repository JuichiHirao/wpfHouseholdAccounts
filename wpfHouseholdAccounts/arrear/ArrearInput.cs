using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace wpfHouseholdAccounts
{
    public class ArrearInputData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
                if (!String.IsNullOrWhiteSpace(value))
                {
                    _DisplayDate = value;
                    DateTime inputdate = Convert.ToDateTime(value);
                    _Date = inputdate;
                }

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
                if (inputdate.Year == 1 || inputdate.Year == 1900)
                    DisplayDate = "";
                else
                    DisplayDate = inputdate.ToString("yyyy/MM/dd");

                _Date = value;
            }
        }

        private string _DisplayPaymentDate;
        public string DisplayPaymentDate
        {
            get
            {
                return _DisplayPaymentDate;
            }
            set
            {
                _DisplayPaymentDate = value;
                NotifyPropertyChanged("DisplayPaymentDate");
            }
        }
        private DateTime _PaymentDate;
        public DateTime PaymentDate
        {
            get
            {
                return _PaymentDate;
            }
            set
            {
                DateTime inputPaymentDate = Convert.ToDateTime(value);
                if (inputPaymentDate.Year == 1 || inputPaymentDate.Year == 1900)
                    DisplayPaymentDate = "";
                else
                    DisplayPaymentDate = inputPaymentDate.ToString("yyyy/MM/dd");

                _PaymentDate = value;
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

        private string _ArrearCode;
        public string ArrearCode
        {
            get
            {
                return _ArrearCode;
            }
            set
            {
                _ArrearCode = value;
                NotifyPropertyChanged("ArrearCode");
            }
        }
        private string _ArrearName;
        public string ArrearName
        {
            get
            {
                return _ArrearName;
            }
            set
            {
                _ArrearName = value;
                NotifyPropertyChanged("ArrearName");
            }
        }

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
                NotifyPropertyChanged("Amount");
            }
        }
        public string Summary { get; set; }
        public ArrearInputData()
        {
            DateTime dt = DateTime.Now;
            Id = Convert.ToInt32(dt.ToString("MMddHHmmss"));
            ArrearCode = "";
            DebitCode = "";
            Amount = 0;
            Summary = "";
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public string CreditKind;		// 借方科目種別
        public string DebitKind;		// 貸方科目種別
    }

    public class ArrearInput
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void CheckData(List<ArrearInputData> myList, Account myAccount)
        {
            foreach (ArrearInputData data in myList)
            {
                if (data.DebitCode == null || data.ArrearCode == null)
                    throw new BussinessException("科目コードが未入力な行が存在します");

                if (data.DebitCode.Length <= 0 && data.ArrearCode.Length <= 0)
                    throw new BussinessException("科目コードが未入力な行が存在します");

                string Name = myAccount.getName(data.DebitCode);

                if (Name.Length <= 0)
                    throw new BussinessException("無効な科目コード[" + data.DebitCode + "]が入力されています");

                Name = myAccount.getName(data.ArrearCode);

                if (Name.Length <= 0)
                    throw new BussinessException("無効な未払コード[" + data.ArrearCode + "]が入力されています");

                AccountData accountData = myAccount.GetItemFromCode(data.DebitCode);

                if (accountData != null && accountData.CrucialFlag)
                {
                    if (data.Summary.Trim().Length <= 0)
                        throw new BussinessException("摘要必須の科目コード[" + data.DebitCode + "]に摘要が入力されていません");
                }

                accountData = myAccount.GetItemFromCode(data.ArrearCode);

                if (accountData != null && accountData.CrucialFlag)
                {
                    if (data.Summary.Trim().Length <= 0)
                        throw new BussinessException("摘要必須の未払コード[" + data.ArrearCode + "]に摘要が入力されていません");
                }

            }
            return;
        }

        public static void ExportXml(string myFilename, List<ArrearInputData> myData)
        {
            XElement root = new XElement("ArrearInputData");

            if (myData != null)
            {
                foreach (ArrearInputData data in myData)
                {
                    if (data == null)
                        continue;

                    if (data.Date == null
                        || data.DebitCode == null
                        || data.ArrearCode == null)
                        continue;

                    if (data.Date.Year <= 1900
                        && data.DebitCode.Length <= 0
                        && data.ArrearCode.Length <= 0)
                        continue;

                    root.Add(new XElement("ArrerInput"
                                        , new XElement("年月日", data.Date)
                                        , new XElement("借方コード", data.DebitCode)
                                        , new XElement("借方名", data.DebitName)
                                        , new XElement("未払コード", data.ArrearCode)
                                        , new XElement("未払名", data.ArrearName)
                                        , new XElement("金額", data.Amount)
                                        , new XElement("摘要", data.Summary)
                                ));
                }
            }

            root.Save(myFilename);
        }

        public static List<ArrearInputData> ImportXml(Environment myEnv)
        {
            string xmlFileName = myEnv.GetXmlSavePathname("XmlArrearInputFileName");

            List<ArrearInputData> listInputData = new List<ArrearInputData>();

            if (!File.Exists(xmlFileName))
            {
                return listInputData;
            }
            XElement root = XElement.Load(xmlFileName);

            var listAll = from element in root.Elements("ArrerInput")
                          select element;

            foreach (XContainer xcon in listAll)
            {
                ArrearInputData inputdata = new ArrearInputData();

                try
                {
                    inputdata.Date = Convert.ToDateTime(xcon.Element("年月日").Value);
                    inputdata.DebitCode = xcon.Element("借方コード").Value;
                    inputdata.DebitName = xcon.Element("借方名").Value;
                    inputdata.ArrearCode = xcon.Element("未払コード").Value;
                    inputdata.ArrearName = xcon.Element("未払名").Value;
                    inputdata.Amount = Convert.ToInt64(xcon.Element("金額").Value);
                    inputdata.Summary = xcon.Element("摘要").Value;
                }
                catch (NullReferenceException nullex)
                {
                    _logger.Debug(nullex);
                    // XML内にElementが存在しない場合に発生、無視する
                }

                listInputData.Add(inputdata);
            }

            return listInputData;
        }

    }
}
