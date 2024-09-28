using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Data.SqlClient;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.ComponentModel;
using NLog;
using System.Text.RegularExpressions;
using wpfHouseholdAccounts.summary;

namespace wpfHouseholdAccounts
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly static RoutedCommand Search = new RoutedCommand("Search", typeof(MainWindow));

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        DbConnection dbcon = new DbConnection();
        Account account;
        List<MakeupData> listMakeup = new List<MakeupData>();
        ICollectionView ColViewListMakeup;
        List<DateTime> listMakeupScopeDate;
        List<int> listMakeupScopeDateYear;
        List<PaymentData> listPaymentDecision;

        List<MakeupDetailData> listInputDataDetail;
        ICollectionView ColViewListInputDataDetail;

        private int dispinfoBeforeGridRowId = 0;
        private string dispinfoTargetTableName = "";
        private string dispinfoTargetSalaryCode = "";
        private DateTime?[] dispctrlMakeupScopeDate;
        private string dispinfoSearchText = "";
        private MakeupDetailData dispinfoSelectDataGridMakeupDetailData;
        private MakeupData dispinfoSelectDataGridMakeupMainData;
        private TravelData dispinfoInputTravelData;

        private bool[] dispinfoMakeupDetailFilterButton = null;
        private double[] dispctrlArrDataGridColumnWidth = null;
        private List<SummaryParameter> listSummaryParameter = null;

        private const int MAKEUPDETAIL_MODE_MAKEUPDETAIL = 1; // 集計からのダブルクリックによる詳細表示
        private const int MAKEUPDETAIL_MODE_ACCOUNTPAYMENT = 2; // 銀行口座の支払情報表示
        private const int MAKEUPDETAIL_MODE_LOANDETAIL = 3; // カード情報の詳細表示
        private const int MAKEUPDETAIL_MODE_SEARCH = 4; // 検索モード
        private const int MAKEUPDETAIL_MODE_SEARCH_ARREAR = 4; // 検索モード
        private int dispctrlMakeupDetailMode = 0;
        private bool dispctrlDataGridRefreshMakeupDetail = false;

        private bool dispinfoMakeupWayUpperOnly = false;
        private int dispinfoMakeupWayUnionKind = 0;

        private int dispinfoMakeupWayKind = 0;

        private bool _isEnterPressed;

        //////////////////////////////////////////
        // ストアドプロシージャ実行用定数：定義 //
        //////////////////////////////////////////
        public const int ST_DO_UNION_UPPERONLY = 2;		// 上位のみ
        public const int ST_DO_UNION_ALL = 1;		// 全て
        public const int GRIDV_TOTAL_USEBUDGET_ON = 1;		// 予算：生活費補充を含める
        public const int GRIDV_TOTAL_USEBUDGET_OFF = 0;		// 予算：生活費補充を含めない

        // グリッド：カラム番号（仕訳詳細JounalDetail）
        public const int GRIDCLM_JD_KIND = 0;		// 種別
        public const int GRIDCLM_JD_INPUTDATE = 1;		// 年月日
        public const int GRIDCLM_JD_DEBITCODE = 2;		// 借方
        public const int GRIDCLM_JD_DEBITNAME = 3;		// 借方科目名
        public const int GRIDCLM_JD_CREDITCODE = 4;		// 貸方
        public const int GRIDCLM_JD_CREDITNAME = 5;		// 貸方科目名
        public const int GRIDCLM_JD_AMOUNT = 6;		// 金額
        public const int GRIDCLM_JD_MAKEUPDATE = 7;		// 集計年月日
        public const int GRIDCLM_JD_REMARK = 8;		// 摘要

        public const string TARGET_SALARY_CODE = "30401";	// 集計の期日対象となる給料日の科目コード

        //   戻るボタンに対応するための操作履歴
        private List<string> dispinfoListCtrlHistory;

        // 集計条件用変数
        private DateTime ConditionFromDate = new DateTime();
        private DateTime ConditionToDate = new DateTime();

        private long TotalProfit = 0;	    // 収益合計
        private long TotalUseBudget = 0;    // 予算：生活費補充合計
        private long TotalExpense = 0;	    // 費用合計
        private long TotalSaving = 0;	    // 貯蓄合計
        private long TotalBadget = 0;	    // 予算合計
        private long TotalNoTarget = 0;	    // 対象外合計
        private long TotalNoTargetMakeup = 0; // 対象外合計集計合算

        public MainWindow()
        {
            InitializeComponent();

            dispctrlMakeupScopeDate = new DateTime?[2];
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CommandBindings.Add(new CommandBinding(Search, (s, ea) => { OnSwitchLayoutSearch(s, ea); }, (s, ea) => ea.CanExecute = true));

            _logger.Debug("Window_Loaded");


            dispinfoMakeupWayUpperOnly = true;

            menuitemUpperOnly.IsChecked = dispinfoMakeupWayUpperOnly;
            dispinfoMakeupWayUnionKind = ST_DO_UNION_UPPERONLY;

            dbcon.openConnection();

            account = new Account();

            // 集計対象のテーブルが金銭帳 or 金銭帳SZEを設定
            SetDbTableInfo();

            SetSaralyDate();

            SetConditionDate();

            // 支払確定基準日の取得、設定
            Environment env = new Environment();
            DateTime BaseDate;
            try
            {
                BaseDate = Convert.ToDateTime(env.GetValue("支払確定集計基準日"));
            }
            catch (Exception)
            {
                BaseDate = new DateTime(1901, 1, 1);
            }

            MoneyNowParent parent = new MoneyNowParent();
            //List<MoneyNowData> listNowData = parent.listMoneyNowData;
            parent.SetInfo(account);
            parent.Calculate(DateTime.Now, BaseDate, Payment.GridvDecisionPaymentSetData());

            dgridMakeupNowInfo.ItemsSource = parent.listMoneyNowData;

            long totalMinusAmount = 0;
            foreach (MoneyNowData data in parent.listMoneyNowData)
            {
                if (data.BaseDateBalanceAmount < 0)
                {
                    totalMinusAmount = totalMinusAmount + (data.BaseDateBalanceAmount * -1);
                }
            }

            TxtbBalanceMinus.Text = String.Format("-{0:###,###,##0}", totalMinusAmount);

            // SQLServerの日付を取得して、サーバー側の日付がずれていたら背景を赤にする
            DateTime dtDbNow = dbcon.getDateStringSql("SELECT GETDATE()");
            TimeSpan span = dtDbNow - DateTime.Now;
            if (span.Days > 30 || span.Days < -30)
                this.Background = Brushes.Red;

            dbcon.closeConnection();

            List<AccountData> listAccount = account.GetItems();

            // 支払確定情報の表示
            RefreshDataGridPaymentSchedule();

            dispctrlDataGridRefreshMakeupDetail = true;
            SwitchLayout(0);

            // 金銭帳入力データの取得
            // 表示データの設定
            listInputDataDetail = MoneyInput.GetInputDetailAll(dbcon);
            dgridMakeupDetail.ItemsSource = listInputDataDetail;

            ColViewListInputDataDetail = CollectionViewSource.GetDefaultView(listInputDataDetail);
            dispctrlDataGridRefreshMakeupDetail = false;

            lgridSummary.Visibility = Visibility.Visible;
            SetDataSetMakeup();
        }

        /// <summary>
        /// 子画面から画面情報更新用のメソッド
        /// </summary>
        public void OwnerRefreshDisplayData()
        {
            RefreshDataGridPaymentSchedule();
        }

        public PaymentData OwnerGetPaymentSchedule(string myCreditCode, DateTime myPaymentDate)
        {
            PaymentData matchdata = null;

            foreach(PaymentData data in listPaymentDecision)
            {
                if (data.DebitCode.Equals(myCreditCode)
                    && data.PaymentDate.CompareTo(myPaymentDate) == 0)
                {
                    matchdata = data;
                    break;
                }
            }

            return matchdata;
        }

        private void RefreshDataGridPaymentSchedule()
        {
            // 支払確定情報の取得、画面に反映
            listPaymentDecision = Payment.GridvDecisionPaymentSetData();
            dgridPaymentSchedule.ItemsSource = listPaymentDecision;
            // 支払確定の行の色を設定
            SetRowColorPaymentDesicion();
        }

        /// <summary>
        /// 支払確定の各行の色を設定する
        /// </summary>
        private void SetRowColorPaymentDesicion()
        {
            if (dgridPaymentSchedule.ItemsSource == null)
                return;
            // 支払確定の色情報の設定
            long total = 0, efectivetotal = 0;
            DbConnection dbcon = new DbConnection();
            foreach (PaymentData data in dgridPaymentSchedule.ItemsSource)
            {
                // なぜかUpdateLayoutを実行しないとrowの戻りがnullになる
                // http://stackoverflow.com/questions/6713365/itemcontainergenerator-containerfromitem-returns-null EverClip
                dgridPaymentSchedule.UpdateLayout();

                var row = dgridPaymentSchedule.ItemContainerGenerator.ContainerFromItem(data) as DataGridRow;

                // 過去の支払確定済みの行はLightGrayで設定
                if (data.DbExistCheck(dbcon))
                {
                    if (row != null)
                        row.Background = Brushes.LightGray;
                }
                else
                    efectivetotal += data.Amount;

                total += data.Amount;
            }
            txtbPaymentScheduleInfo.Text = "未払 " + String.Format("{0:###,###,##0}", efectivetotal) + " ／ " + "合計 " + String.Format("{0:###,###,##0}", total);
        }

        private double CalcurateColumnWidth(DataGrid datagrid)
        {
            double winX = lgridMoneyInputDetail.ActualWidth - 10;
            double colTotal = 0;
            foreach (DataGridColumn col in datagrid.Columns)
            {
                if (col.Header.Equals("摘要"))
                    continue;

                DataGridLength colw = col.Width;
                double w = colw.DesiredValue;
                colTotal += w;
            }

            return winX - colTotal - 25; // ScrollBarが表示されない場合は8
        }

        private void DataGridMakeupDetailWidthSetting()
        {
            txtbMakeupDetailLoanInfo.Text = "";
            if (dispctrlMakeupDetailMode == MAKEUPDETAIL_MODE_MAKEUPDETAIL)
            {
                dgridMakeupDetail.Width = lgridMoneyInputDetail.ActualWidth - 10;
                
                if (dispctrlArrDataGridColumnWidth != null)
                {
                    int idx = 0;
                    foreach (DataGridColumn col in dgridMakeupDetail.Columns)
                    {
                        col.Width = new DataGridLength(dispctrlArrDataGridColumnWidth[idx]);
                        idx++;
                    }
                }
                dgridMakeupDetail.Columns[8].Width = new DataGridLength(0);
                dgridMakeupDetail.Columns[8].Visibility = System.Windows.Visibility.Hidden;
                dgridMakeupDetail.Columns[7].Width = new DataGridLength(CalcurateColumnWidth(dgridMakeupDetail));

                if (dispctrlArrDataGridColumnWidth == null)
                {
                    dispctrlArrDataGridColumnWidth = new double[dgridMakeupDetail.Columns.Count];
                    int idx = 0;
                    foreach (DataGridColumn col in dgridMakeupDetail.Columns)
                    {
                        dispctrlArrDataGridColumnWidth[idx] = col.Width.DesiredValue;
                        idx++;
                    }
                }
            }
            else if (dispctrlMakeupDetailMode == MAKEUPDETAIL_MODE_ACCOUNTPAYMENT
                        || dispctrlMakeupDetailMode == MAKEUPDETAIL_MODE_LOANDETAIL)
            {
                dgridMakeupDetail.Width = lgridMoneyInputDetail.ActualWidth - 10;
                if (dispctrlArrDataGridColumnWidth != null)
                {
                    int idx = 0;
                    foreach (DataGridColumn col in dgridMakeupDetail.Columns)
                    {
                        col.Width = new DataGridLength(dispctrlArrDataGridColumnWidth[idx]);
                        idx++;
                    }
                }
                dgridMakeupDetail.Columns[8].Width = new DataGridLength(0);
                dgridMakeupDetail.Columns[8].Visibility = System.Windows.Visibility.Hidden;
                dgridMakeupDetail.Columns[7].Width = new DataGridLength(CalcurateColumnWidth(dgridMakeupDetail));

                if (dispctrlMakeupDetailMode == MAKEUPDETAIL_MODE_LOANDETAIL)
                    txtbMakeupDetailLoanInfo.Text = dispinfoSelectDataGridMakeupDetailData.DebitName + " " + dispinfoSelectDataGridMakeupDetailData.Date.ToString("yyyy/MM/dd") + " " + String.Format("{0:###,###,##0}", dispinfoSelectDataGridMakeupDetailData.Amount);

            }
            else if (dispctrlMakeupDetailMode == MAKEUPDETAIL_MODE_SEARCH)
            {
                dgridMakeupDetail.Width = lgridMoneyInputDetail.ActualWidth - 10;
                dgridMakeupDetail.Columns[3].Width = new DataGridLength(180);
                dgridMakeupDetail.Columns[5].Width = new DataGridLength(180);
                dgridMakeupDetail.Columns[6].Width = new DataGridLength(100);
                dgridMakeupDetail.Columns[8].Visibility = System.Windows.Visibility.Visible;
                dgridMakeupDetail.Columns[8].Width = new DataGridLength(150);
                dgridMakeupDetail.Columns[7].Width = new DataGridLength(CalcurateColumnWidth(dgridMakeupDetail));
            }
            else if (dispctrlMakeupDetailMode == MAKEUPDETAIL_MODE_SEARCH_ARREAR)
            {
                dgridMakeupDetail.Columns[0].Width = new DataGridLength(50);
                dgridMakeupDetail.Width = lgridMoneyInputDetail.ActualWidth - 10;
                dgridMakeupDetail.Columns[3].Width = new DataGridLength(180);
                dgridMakeupDetail.Columns[5].Width = new DataGridLength(180);
                dgridMakeupDetail.Columns[6].Width = new DataGridLength(100);
                dgridMakeupDetail.Columns[8].Visibility = System.Windows.Visibility.Visible;
                dgridMakeupDetail.Columns[8].Width = new DataGridLength(150);
                dgridMakeupDetail.Columns[7].Width = new DataGridLength(CalcurateColumnWidth(dgridMakeupDetail));
            }
        }

        private void SetViewFilterAndSort()
        {
            bool isWithCompanyCard = false;
            object oWithCompanyCard = chkWithCompanyCard.IsChecked;

            if (oWithCompanyCard != null)
                isWithCompanyCard = Convert.ToBoolean(oWithCompanyCard);

            if (dispctrlDataGridRefreshMakeupDetail)
            {
                // 金銭帳入力データの取得
                // 表示データの設定
                listInputDataDetail = MoneyInput.GetInputDetailAll(dbcon);
                dgridMakeupDetail.ItemsSource = listInputDataDetail;

                ColViewListInputDataDetail = CollectionViewSource.GetDefaultView(listInputDataDetail);
                dispctrlDataGridRefreshMakeupDetail = false;

            }

            if (dispctrlMakeupDetailMode == MAKEUPDETAIL_MODE_MAKEUPDETAIL)
            {
                bool IsExpenseKind = false;
                string expensekind = "";
                Regex regex = new Regex("^[3-6][0-9] ");

                if (dispinfoSelectDataGridMakeupMainData != null && regex.IsMatch(dispinfoSelectDataGridMakeupMainData.AccountUpperCode))
                {
                    IsExpenseKind = true;
                    expensekind = regex.Match(dispinfoSelectDataGridMakeupMainData.AccountUpperCode).Value.Trim();
                }

                ColViewListInputDataDetail.Filter = delegate(object o)
                {
                    MakeupDetailData data = o as MakeupDetailData;
                    bool IsMatch = false;
                    bool IsMatchWayKind = false;
                    if (  (data.Date.CompareTo(ConditionFromDate) >= 0
                             && data.Date.CompareTo(ConditionToDate) <= 0)
                       || (data.RegistDate.CompareTo(ConditionFromDate) >= 0
                             && data.RegistDate.CompareTo(ConditionToDate) <= 0))
                    {
                        if (dispinfoMakeupWayKind == MAKEUPWAY_KIND_SZE)
                        {
                            if (data.Kind == 3)
                            {
                                Debug.Print("data.Date [" + data.Date.ToShortDateString() + "]   data.DebitCode [" + data.DebitCode + "]  data.Remark [" +  data.Remark + "]   dispinfoMakeupWayKind [" + dispinfoMakeupWayKind + "]    data.Kind [" + data.Kind + "]");
                                IsMatchWayKind = true;
                            }
                        }
                        else if (dispinfoMakeupWayKind == MAKEUPWAY_KIND_CARD)
                        {
                            string kind = account.getAccountKind(data.CreditCode);

                            if (data.Kind == 2 && kind.Equals(Account.KIND_DEPT_LOAN))
                                IsMatchWayKind = true;
                        }
                        else if (dispinfoMakeupWayKind == MAKEUPWAY_KIND_CASH)
                        {
                            if (data.Kind == 1 && data.CreditCode.Equals(Account.CODE_CASH))
                                IsMatchWayKind = true;

                        }
                        else if (dispinfoMakeupWayKind == MAKEUPWAY_KIND_COMPANY)
                        {
                            if (data.CreditCode.IndexOf("31") == 0)
                                IsMatchWayKind = true;

                        }
                        else
                            IsMatchWayKind = true;

                        if (IsExpenseKind)
                        {
                            string kind = account.getAccountKind(data.DebitCode);

                            if (expensekind.Equals(kind) && IsMatchWayKind)
                                IsMatch = true;
                        }
                        if (dispinfoSelectDataGridMakeupMainData.AccountUpperCode.Equals("総合計"))
                        {
                            if (dispinfoMakeupWayKind == MAKEUPWAY_KIND_CARD)
                            {
                                string kind = account.getAccountKind(data.CreditCode);

                                if (data.Kind == 2 && kind.Equals(Account.KIND_DEPT_LOAN))
                                    return true;
                                else
                                    return false;
                            }
                            else
                                return true;
                        }
                        if (dispinfoSelectDataGridMakeupMainData.AccountUpperCode.Equals("費用合計"))
                        {
                            string kind = account.getAccountKind(data.DebitCode);

                            if (kind.Equals(Account.KIND_EXPENSE) && IsMatchWayKind)
                                return true;
                        }
                        if (dispinfoSelectDataGridMakeupMainData.AccountUpperCode.Equals("収益合計"))
                        {
                            string kind = account.getAccountKind(data.CreditCode);

                            if (kind.Equals(Account.KIND_PROFIT) && IsMatchWayKind)
                                return true;
                        }
                        //Debug.Print("data.Date [" + data.Date.ToShortDateString() + "]    ConditionFromDate [" + ConditionFromDate.ToShortDateString() + "]");
                        if ( (data.DebitUpperCode.Length > 0 && (data.DebitUpperCode.Equals(dispinfoSelectDataGridMakeupMainData.AccountCode)
                            || data.DebitUpperCode.Equals(dispinfoSelectDataGridMakeupMainData.AccountUpperCode)))
                            || (data.DebitCode.Length > 0 && (data.DebitCode.Equals(dispinfoSelectDataGridMakeupMainData.AccountCode)
                            || data.DebitCode.Equals(dispinfoSelectDataGridMakeupMainData.AccountUpperCode))))
                        {
                            //Debug.Print("data.DebitCode [" + data.DebitCode + "]    data.DebitUpperCode [" + data.DebitCode + "]" + "  dispinfoSelectDataGridMakeupMainData.AccountCode [" + dispinfoSelectDataGridMakeupMainData.AccountUpperCode + "]    dispinfoSelectDataGridMakeupMainData.AccountUpperCode [" + dispinfoSelectDataGridMakeupMainData.AccountUpperCode + "]");
                            if (IsMatchWayKind)
                                IsMatch = true;
                        }

                        if ((data.CreditUpperCode.Length > 0 && (data.CreditUpperCode.Equals(dispinfoSelectDataGridMakeupMainData.AccountCode)
                            || data.CreditUpperCode.Equals(dispinfoSelectDataGridMakeupMainData.AccountUpperCode)))
                            || (data.CreditCode.Length > 0 && (data.CreditCode.Equals(dispinfoSelectDataGridMakeupMainData.AccountCode)
                            || data.CreditCode.Equals(dispinfoSelectDataGridMakeupMainData.AccountUpperCode))))
                        {
                            //Debug.Print("data.DebitCode [" + data.DebitCode + "]    data.DebitUpperCode [" + data.DebitCode + "]" + "  dispinfoSelectDataGridMakeupMainData.AccountCode [" + dispinfoSelectDataGridMakeupMainData.AccountUpperCode + "]    dispinfoSelectDataGridMakeupMainData.AccountUpperCode [" + dispinfoSelectDataGridMakeupMainData.AccountUpperCode + "]");
                            if (IsMatchWayKind)
                                IsMatch = true;
                        }

                    }

                    return IsMatch;
                };
                ColViewListInputDataDetail.SortDescriptions.Clear();
                ColViewListInputDataDetail.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            }
            else if (dispctrlMakeupDetailMode == MAKEUPDETAIL_MODE_ACCOUNTPAYMENT)
            {
                ColViewListInputDataDetail.Filter = delegate(object o)
                {
                    MakeupDetailData data = o as MakeupDetailData;

                    //Debug.Print("dispinfoFilterCreditCode [" + dispinfoFilterCreditCode + "]    data.CreditCode [" + data.CreditCode + "]");
                    bool IsMatch = false;
                    if (data.Date.CompareTo(ConditionFromDate) >= 0
                        && data.Date.CompareTo(ConditionToDate) <= 0)
                    {
                        if (data.CreditCode.Equals("10208")
                            && !data.DebitCode.Equals("10100"))
                            IsMatch = true;
                    }

                    return IsMatch;
                };
                ColViewListInputDataDetail.SortDescriptions.Clear();
                ColViewListInputDataDetail.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            }
            else if (dispctrlMakeupDetailMode == MAKEUPDETAIL_MODE_LOANDETAIL)
            {
                DateTime seldataStart = dispinfoSelectDataGridMakeupDetailData.Date.AddDays(-10);
                DateTime seldataEnd = dispinfoSelectDataGridMakeupDetailData.Date.AddDays(10);
                ;
                ColViewListInputDataDetail.Filter = delegate(object o)
                {
                    MakeupDetailData data = o as MakeupDetailData;

                    //Debug.Print("dispinfoFilterCreditCode [" + dispinfoFilterCreditCode + "]    data.CreditCode [" + data.CreditCode + "]");
                    bool IsMatch = false;
                    if (data.RegistDate.CompareTo(seldataStart) >= 0
                        && data.RegistDate.CompareTo(seldataEnd) <= 0)
                    {
                        if (data.Kind == 2 && data.CreditCode.Equals(dispinfoSelectDataGridMakeupDetailData.DebitCode))
                            IsMatch = true;
                    }

                    return IsMatch;
                };
            }
            else if (dispctrlMakeupDetailMode == MAKEUPDETAIL_MODE_SEARCH)
            {
                List<SearchInfo> listSearchInfo = new List<SearchInfo>();

                string[] arrSearchText = dispinfoSearchText.Split(' ');
                if (dispinfoSearchText != null && dispinfoSearchText.Length > 0)
                {
                    foreach (string searchtext in arrSearchText)
                    {
                        SearchInfo searchinfo = new SearchInfo(searchtext);

                        listSearchInfo.Add(searchinfo);
                    }
                }

                // Debug.Print("dispinfoMakeupDetailFilterButton " + dispinfoMakeupDetailFilterButton.ToString());
                ColViewListInputDataDetail.Filter = delegate(object o)
                {
                    MakeupDetailData data = o as MakeupDetailData;

                    //Debug.Print("dispinfoFilterCreditCode [" + dispinfoFilterCreditCode + "]    data.CreditCode [" + data.CreditCode + "]");

                    int matchCount = 0; // AND検索のため、検索条件の全てに一致する事のチェックのためにマッチ数をカウント
                    bool IsMatch = false;

                    foreach(SearchInfo search in listSearchInfo)
                    {
                        if (search.Text.Length <= 0)
                            continue;

                        if (search.Kind == SearchInfo.KIND_TEXT)
                        {
                            if (data.CreditCode.IndexOf(search.Text) >= 0
                                || data.CreditName.IndexOf(search.Text) >= 0
                                || data.DebitCode.IndexOf(search.Text) >= 0
                                || data.DebitName.IndexOf(search.Text) >= 0
                                || data.Remark.IndexOf(search.Text) >= 0)
                                matchCount++;
                        }

                        if (search.Kind == SearchInfo.KIND_DATERANGE)
                        {
                            if (data.Date.CompareTo(search.FromDate) >= 0
                                && data.Date.CompareTo(search.ToDate) <= 0)
                                matchCount++;
                        }

                        if (search.Kind == SearchInfo.KIND_DATE)
                        {
                            if (data.Date.CompareTo(search.FromDate) == 0)
                                matchCount++;
                        }
                    }

                    if (matchCount == listSearchInfo.Count)
                        IsMatch = true;

                    if (dispinfoMakeupDetailFilterButton != null)
                    {
                        if (dispinfoMakeupDetailFilterButton[0] && data.Kind == 1)
                        {
                            if (data.CreditCode.IndexOf("31") == 0 || data.CreditCode.Equals("20801")
                            || data.UsedCompanyArrear == 1)
                                IsMatch = false;
                            else
                                IsMatch = true;
                            /*
                            if (!data.IsCompany())
                                IsMatch = true;
                            else
                                IsMatch = false;
                             */
                            //Debug.Print("Match!! IsMatch [" + IsMatch + "]  dispinfoMakeupDetailFilterButton[0] [" + dispinfoMakeupDetailFilterButton[0] + "]  data.Kind [" + data.Kind + "]");
                        }
                        else if (data.Kind == 1)
                            IsMatch = false;

                        if (dispinfoMakeupDetailFilterButton[1] && data.Kind == 2)
                            IsMatch = true;
                        else if (data.Kind == 2)
                            IsMatch = false;
                        // Debug.Print("Match!! 2 IsMatch [" + IsMatch + "]");

                        if (dispinfoMakeupDetailFilterButton[2] && data.Kind == 3)
                            IsMatch = true;
                        else if (data.Kind == 3)
                            IsMatch = false;
                        // Debug.Print("Match!! 3 IsMatch [" + IsMatch + "]");

                        if (dispinfoMakeupDetailFilterButton[3])
                        {
                            if (data.IsCompany())
                                IsMatch = true;
                            else
                                IsMatch = false;
                        }
                        // Debug.Print("Match!! 4 IsMatch [" + IsMatch + "]");

                        /*
                        if (isWithCompanyCard)
                        {
                            if (dispinfoMakeupDetailFilterButton[4] && data.Kind == 4)
                                IsMatch = true;
                            else
                            {
                                if (dispinfoMakeupDetailFilterButton[4]
                                    && data.DebitCode == "20801" || (data.CreditCode == "20801" && data.Kind == 2))
                                    IsMatch = true;
                                else
                                {
                                    if (dispinfoMakeupDetailFilterButton[1] && data.Kind == 2)
                                        IsMatch = true;
                                    else
                                        IsMatch = false;
                                }
                            }
                        }
                        else
                        {
                            if (dispinfoMakeupDetailFilterButton[4] && data.Kind == 4)
                                IsMatch = true;
                            else
                                IsMatch = false;
                        }
                         */

                        //Debug.Print("Match!! 5 IsMatch [" + IsMatch + "]");

                        if (dispinfoMakeupDetailFilterButton[5] && data.Kind == 5)
                            IsMatch = true;
                        else if (data.Kind == 5)
                            IsMatch = false;
                        // Debug.Print("Match!! 6 IsMatch [" + IsMatch + "]");

                        if (data.Kind == 4 || data.Kind == 6)
                            IsMatch = false;


                    }
                    //Debug.Print("IsMatch [" + IsMatch + "]  dispinfoMakeupDetailFilterButton[0] [" + dispinfoMakeupDetailFilterButton[0] + "]  data.Kind [" + data.Kind + "]");

                    return IsMatch;
                };

                if (dispinfoMakeupDetailFilterButton != null && dispinfoMakeupDetailFilterButton[4])
                {
                    ColViewListInputDataDetail.SortDescriptions.Clear();
                    ColViewListInputDataDetail.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
                    ColViewListInputDataDetail.SortDescriptions.Add(new SortDescription("DataOrder", ListSortDirection.Ascending));

                    MakeupDetailData defaultMakeupData = new MakeupDetailData();
                    foreach (MakeupDetailData data in ColViewListInputDataDetail)
                    {
                        if (data.DataOrder == 0)
                        {
                            defaultMakeupData = data;
                            break;
                        }
                    }

                    long balance = 0;

                    balance = defaultMakeupData.Balance;
                    int DataOrder = 1;
                    foreach (MakeupDetailData data in ColViewListInputDataDetail)
                    {
                        if (data.DataOrder == 0)
                            continue;

                        if (data.DebitCode.IndexOf("1201") == 0
                            || data.DebitCode == "20801")
                            balance = balance - data.Amount;
                        else
                            balance = balance + data.Amount;

                        if (data.DataOrder != DataOrder || data.Balance != balance)
                        {
                            Debug.Print("No[" + data.DataOrder + "] balance [" + balance + "]");
                            data.DataOrder = DataOrder;
                            data.Balance = balance;
                            //Arrear.Update(data, dbcon);
                        }
                        //else
                        //    Debug.Print("No[" + data.DataOrder + "] balance [" + balance + "]");

                        DataOrder++;
                    }
                }
                else
                {
                    ColViewListInputDataDetail.SortDescriptions.Clear();
                    ColViewListInputDataDetail.SortDescriptions.Add(new SortDescription("RegistDate", ListSortDirection.Descending));
                }
            }

            DataGridMakeupDetailWidthSetting();
        }

        private const int LAYOUTMODE_DEFAULT = 1;
        private const int LAYOUTMODE_MONEYINPUTDETAIL = 2;
        private const int LAYOUTMODE_SEARCH = 3;
        private const int LAYOUTMODE_SEARCHEXECUTE = 4;
        private const int LAYOUTMODE_MAKEUP_TARGET_DETAIL = 5;

        public void SwitchLayout(int myMode)
        {
            // 初期表示時
            //   現在情報、支払確定が表示、金銭帳情報は非表示

            // 集計をダブルクリック
            //   金銭帳情報は表示、現在情報、支払確定が非表示
            if (myMode == 0)
                myMode = LAYOUTMODE_DEFAULT;

            // 集計表の表示
            if (myMode != LAYOUTMODE_SEARCH)
            {
                dgridMakeupDetail.SetValue(Grid.RowProperty, 2);
                dgridMakeupDetail.SetValue(Grid.RowSpanProperty, 1);

                lgridMakeupControl.Visibility = System.Windows.Visibility.Visible;
                lgridSummary.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                // Grid.Row="1" Grid.RowSpan="2"
                dgridMakeupDetail.SetValue(Grid.RowProperty, 1);
                dgridMakeupDetail.SetValue(Grid.RowSpanProperty, 2);
                lgridMakeupControl.Visibility = System.Windows.Visibility.Hidden;
                lgridSummary.Visibility = System.Windows.Visibility.Hidden;
            }

            if (myMode == LAYOUTMODE_DEFAULT)
            {
                dgridMakeupNowInfo.Visibility = System.Windows.Visibility.Visible;
                lgridPaymentSchedule.Visibility = System.Windows.Visibility.Visible;
                dispctrlMakeupDetailMode = 0;
            }
            else
            {
                dgridMakeupNowInfo.Visibility = System.Windows.Visibility.Hidden;
                lgridPaymentSchedule.Visibility = System.Windows.Visibility.Hidden;
            }

            if (myMode == LAYOUTMODE_MONEYINPUTDETAIL)
            {
                lgridMoneyInputDetail.Visibility = System.Windows.Visibility.Visible;
                dgridMakeupDetail.Visibility = System.Windows.Visibility.Visible;
                lgridMakeupDetailControl.Visibility = System.Windows.Visibility.Visible;

                SetViewFilterAndSort();

                foreach (DataGridColumn col in dgridMakeupDetail.Columns)
                {
                    string header = col.Header.ToString();
                    if (header.Equals("借CD")
                        || header.Equals("貸CD")
                        || header.Equals("金額")
                        || header.Equals("年月日"))
                        col.IsReadOnly = false;
                }
            }
            else
            {
                lgridMoneyInputDetail.Visibility = System.Windows.Visibility.Hidden;
                dgridMakeupDetail.Visibility = System.Windows.Visibility.Hidden;
                lgridMakeupDetailControl.Visibility = System.Windows.Visibility.Hidden;
            }

            if (myMode == LAYOUTMODE_MAKEUP_TARGET_DETAIL)
            {
                lgridMoneyInputDetail.Visibility = System.Windows.Visibility.Visible;
                dgridMakeupDetail.Visibility = System.Windows.Visibility.Visible;
                lgridMakeupDetailControl.Visibility = System.Windows.Visibility.Visible;

                foreach (DataGridColumn col in dgridMakeupDetail.Columns)
                {
                    string header = col.Header.ToString();
                    if (header.Equals("借CD")
                        || header.Equals("貸CD")
                        || header.Equals("金額")
                        || header.Equals("年月日"))
                        col.IsReadOnly = false;
                }
            }
            else
            {
                lgridMoneyInputDetail.Visibility = System.Windows.Visibility.Hidden;
                dgridMakeupDetail.Visibility = System.Windows.Visibility.Hidden;
                lgridMakeupDetailControl.Visibility = System.Windows.Visibility.Hidden;
            }

            if (myMode == LAYOUTMODE_SEARCH)
            {
                lgridMain.RowDefinitions[0].Height = new GridLength(70);

                lgridMoneyInputDetail.Visibility = System.Windows.Visibility.Visible;
                dgridMakeupDetail.Visibility = System.Windows.Visibility.Visible;

                lgridMoneyInputDetail.SetValue(Grid.ColumnProperty, 0);
                lgridMoneyInputDetail.SetValue(Grid.ColumnSpanProperty, 2);

                lgridMoneyInputDetail.RowDefinitions[0].Height = new GridLength(0);

                SetViewFilterAndSort();

                foreach (DataGridColumn col in dgridMakeupDetail.Columns)
                {
                    string header = col.Header.ToString();
                    if (header.Equals("借CD")
                        || header.Equals("貸CD")
                        || header.Equals("金額")
                        || header.Equals("年月日"))
                        col.IsReadOnly = false;
                }
            }
            else
            {
                lgridMain.RowDefinitions[0].Height = new GridLength(0);

                //lgridMoneyInputDetail.Visibility = System.Windows.Visibility.Hidden;
                //dgridMakeupDetail.Visibility = System.Windows.Visibility.Hidden;

                lgridMoneyInputDetail.RowDefinitions[0].Height = new GridLength(60);

                lgridMoneyInputDetail.SetValue(Grid.ColumnProperty, 1);
                lgridMoneyInputDetail.SetValue(Grid.ColumnSpanProperty, 1);
            }
        }

        private void OnSwitchLayoutSearch(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lgridMoneyInputDetail.Visibility == System.Windows.Visibility.Visible)
                {
                    menuitemSearch.IsChecked = false;
                    SwitchLayout(0);
                }
                else
                {
                    menuitemSearch.IsChecked = true;
                    dispctrlMakeupDetailMode = MAKEUPDETAIL_MODE_SEARCH;
                    SwitchLayout(LAYOUTMODE_SEARCH);
                }
            }
            catch(Exception ex)
            {
                _logger.ErrorException("画面のエラー", ex);
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
                MessageBox.Show(ex.StackTrace);
            }

        }

        private void OnSwitchLayoutDefault(object sender, RoutedEventArgs e)
        {
            SwitchLayout(0);
        }

        private void OnSwitchLayoutClose(object sender, RoutedEventArgs e)
        {
            lborderSetMakeupScopeDate.Visibility = System.Windows.Visibility.Hidden;

            if (lborderSetTravel.Visibility == System.Windows.Visibility.Visible)
            {
                lborderSetTravel.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        public void SetSaralyDate()
        {
            if (dgridSelectDate == null)
                return;

            dgridSelectDate.ItemsSource = null;

            DbConnection dbcon = new DbConnection();
            string sqlcmd;
            SqlDataReader reader;

            sqlcmd = "SELECT CONVERT(varchar(10), 年月日,111) AS 給料日 FROM " + dispinfoTargetTableName + " ";
            sqlcmd = sqlcmd + "WHERE 貸方 = @給料コード ";
            sqlcmd = sqlcmd + "ORDER BY 年月日 DESC ";

            SqlCommand scmd = new SqlCommand(sqlcmd, dbcon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@給料コード", SqlDbType.VarChar);
            sqlparams[0].Value = dispinfoTargetSalaryCode;

            dbcon.SetParameter(sqlparams);

            reader = dbcon.GetExecuteReader(sqlcmd);

            listMakeupScopeDate = new List<DateTime>();
            listMakeupScopeDateYear = new List<int>();
            int beforeyear = 0;
            while (reader.Read())
            {
                string data = DbExportCommon.GetDbString(reader, 0);
                DateTime dt = Convert.ToDateTime(data);

                listMakeupScopeDate.Add(dt);

                if (dt.Year != beforeyear)
                    listMakeupScopeDateYear.Add(dt.Year);
                beforeyear = dt.Year;
            }

            reader.Close();

            dgridSelectDate.Items.Clear();
            dgridSelectDate.IsReadOnly = true;
            dgridSelectDate.CanUserAddRows = false;
            dgridSelectDate.CanUserDeleteRows = false;
            dgridSelectDate.CanUserSortColumns = false;

            dgridSelectDate.ItemsSource = listMakeupScopeDate;
            cmbSetMakeupScopeDateYear.ItemsSource = listMakeupScopeDateYear;
        }

        private const int MAKEUPWAY_KIND_SZE = 1;
        private const int MAKEUPWAY_KIND_ARREAR = 2;
        private const int MAKEUPWAY_KIND_CARD = 3;
        private const int MAKEUPWAY_KIND_CASH = 4;
        private const int MAKEUPWAY_KIND_COMPANY = 5;

        public void SetDataSetMakeup()
        {
            if (account == null)
                return;

            MakeSummary(ConditionFromDate, ConditionToDate);

            txtbTermDate.Text = ConditionFromDate.ToString("yyyy/MM/dd") + " ～ " + ConditionToDate.ToString("yyyy/MM/dd");
        }

        public void SetViewMakeupFilterAndSort()
        {
            ColViewListMakeup = CollectionViewSource.GetDefaultView(listMakeup);

            Regex regex = new Regex("[0-9][0-9][0-9][0-9][0-9]");
            ColViewListMakeup.Filter = delegate(object o)
            {
                MakeupData data = o as MakeupData;

                if (data.Amount == 0)
                    return false;

                if (regex.IsMatch(data.AccountUpperCode))
                    return true;

                return true;
            };
            //ColViewListInputDataDetail.SortDescriptions.Clear();
            //ColViewListInputDataDetail.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
        }
        public string GetXmlNoTarget(string[] myArrNoTargets)
        {
            string result = "<NoTargets>";
            int len = result.Length;
            foreach(string target in myArrNoTargets)
            {
                if (target.Length > 0)
                    result += "<kind>" + target + "</kind>";
            }

            return result + "</NoTargets>";
        }

        private void AddListRowData(string myAccountUpperCode, string myAccountCode, string myAccountName, long myAmount)
        {
            MakeupData data = new MakeupData();

            data.AccountUpperCode = myAccountUpperCode;
            data.AccountCode = myAccountCode;
            data.AccountName = myAccountName;
            data.Amount = myAmount;

            data.SetRowStyle();

            if (myAccountUpperCode.Equals("総合計"))
                listMakeup.Insert(0, data);
            else
                listMakeup.Add(data);

            return;
        }

        private void AddListMakeupData(DateTime myFromDate
            , DateTime myToDate
            , string myTargetKind
            , string xmlNoTargetKind
            , int myUnion
            , int myTargetBalance)
        {
            SqlCommand cmd = new SqlCommand("MAKEUP_MONEY_TEST", dbcon.getSqlConnection());

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@from_date", SqlDbType.DateTime));
            cmd.Parameters["@from_date"].Value = myFromDate;

            cmd.Parameters.Add(new SqlParameter("@to_date", SqlDbType.DateTime));
            cmd.Parameters["@to_date"].Value = myToDate;

            cmd.Parameters.Add(new SqlParameter("@target_kind", SqlDbType.VarChar));
            cmd.Parameters["@target_kind"].Value = myTargetKind;

            cmd.Parameters.Add(new SqlParameter("@xml_notarget_kind", SqlDbType.VarChar));
            cmd.Parameters["@xml_notarget_kind"].Value = xmlNoTargetKind;

            cmd.Parameters.Add(new SqlParameter("@union_contain", SqlDbType.Int));
            cmd.Parameters["@union_contain"].Value = myUnion;

            cmd.Parameters.Add(new SqlParameter("@target_balance", SqlDbType.Int));
            cmd.Parameters["@target_balance"].Value = myTargetBalance;

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                AddListRowData(
                    DbExportCommon.GetDbString(reader, 0)
                    , DbExportCommon.GetDbString(reader, 1)
                    , DbExportCommon.GetDbString(reader, 2)
                    , DbExportCommon.GetDbMoney(reader, 3));
            }
            reader.Close();

            return;
        }

        private void AddListMakeupSzeData(DateTime myFromDate
            , DateTime myToDate
            , string myTargetKind
            , string[] NoTargetKind
            , int myUnion
            , int myTargetBalance)
        {
            SqlCommand cmd = new SqlCommand("MAKEUP_MONEYSZE", dbcon.getSqlConnection());

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@from_date", SqlDbType.DateTime));
            cmd.Parameters["@from_date"].Value = myFromDate;

            cmd.Parameters.Add(new SqlParameter("@to_date", SqlDbType.DateTime));
            cmd.Parameters["@to_date"].Value = myToDate;

            cmd.Parameters.Add(new SqlParameter("@target_kind", SqlDbType.VarChar));
            cmd.Parameters["@target_kind"].Value = myTargetKind;

            cmd.Parameters.Add(new SqlParameter("@notarget_kind1", SqlDbType.VarChar));
            cmd.Parameters["@notarget_kind1"].Value = NoTargetKind[0];

            cmd.Parameters.Add(new SqlParameter("@notarget_kind2", SqlDbType.VarChar));
            cmd.Parameters["@notarget_kind2"].Value = NoTargetKind[1];

            cmd.Parameters.Add(new SqlParameter("@union_contain", SqlDbType.Int));
            cmd.Parameters["@union_contain"].Value = myUnion;

            cmd.Parameters.Add(new SqlParameter("@target_balance", SqlDbType.Int));
            cmd.Parameters["@target_balance"].Value = myTargetBalance;

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                AddListRowData(
                    DbExportCommon.GetDbString(reader, 0)
                    , DbExportCommon.GetDbString(reader, 1)
                    , DbExportCommon.GetDbString(reader, 2)
                    , DbExportCommon.GetDbMoney(reader, 3));
            }
            reader.Close();

            return;
        }

        private void AddListMakeupDataCreditFilter(DateTime myFromDate
                    , DateTime myToDate
                    , string myTargetKind
                    , string myTargetKindCredit
                    , int myUnion)
        {
            SqlCommand cmd = new SqlCommand("MAKEUP_MONEY_CREDITFILTER", dbcon.getSqlConnection());

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@from_date", SqlDbType.DateTime));
            cmd.Parameters["@from_date"].Value = myFromDate;

            cmd.Parameters.Add(new SqlParameter("@to_date", SqlDbType.DateTime));
            cmd.Parameters["@to_date"].Value = myToDate;

            cmd.Parameters.Add(new SqlParameter("@target_kind", SqlDbType.VarChar));
            cmd.Parameters["@target_kind"].Value = myTargetKind;

            cmd.Parameters.Add(new SqlParameter("@target_kind_credit", SqlDbType.VarChar));
            cmd.Parameters["@target_kind_credit"].Value = myTargetKindCredit;

            cmd.Parameters.Add(new SqlParameter("@union_contain", SqlDbType.Int));
            cmd.Parameters["@union_contain"].Value = myUnion;

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                AddListRowData(
                    DbExportCommon.GetDbString(reader, 0)
                    , DbExportCommon.GetDbString(reader, 1)
                    , DbExportCommon.GetDbString(reader, 2)
                    , DbExportCommon.GetDbMoney(reader, 3));
            }
            reader.Close();

            return;
        }

        private void AddListMakeupDataCard(DateTime myFromDate
            , DateTime myToDate
            , string myTargetKind
            , int myUnion)
        {
            SqlCommand cmd = new SqlCommand("MAKEUP_MONEY_CARD", dbcon.getSqlConnection());

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@from_date", SqlDbType.DateTime));
            cmd.Parameters["@from_date"].Value = myFromDate;

            cmd.Parameters.Add(new SqlParameter("@to_date", SqlDbType.DateTime));
            cmd.Parameters["@to_date"].Value = myToDate;

            cmd.Parameters.Add(new SqlParameter("@target_kind", SqlDbType.VarChar));
            cmd.Parameters["@target_kind"].Value = myTargetKind;

            cmd.Parameters.Add(new SqlParameter("@union_contain", SqlDbType.Int));
            cmd.Parameters["@union_contain"].Value = myUnion;

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                AddListRowData(
                    DbExportCommon.GetDbString(reader, 0)
                    , DbExportCommon.GetDbString(reader, 1)
                    , DbExportCommon.GetDbString(reader, 2)
                    , DbExportCommon.GetDbMoney(reader, 3));
            }
            reader.Close();

            return;
        }

        private void AddCalcrateTotal(long myTotalUseBadget, string mySubject, string mySelectKind)
        {
            int UseBudgetFlag = MainWindow.GRIDV_TOTAL_USEBUDGET_ON;

            long lngTotalAmount = 0;
            string AccountCodeKind = "";

            foreach (MakeupData data in listMakeup)
            {
                // 金額を合計にインクリメント
                if (data.AccountCode.Length == 0)
                {
                    AccountCodeKind = account.getAccountKind(data.AccountUpperCode);

                    if (AccountCodeKind == mySelectKind)
                        lngTotalAmount += System.Convert.ToInt64(data.Amount);
                }
            }

            if (mySubject.Equals("収益合計") && UseBudgetFlag == MainWindow.GRIDV_TOTAL_USEBUDGET_ON)
            {
                // 合計の行を追加
                AddListRowData("予算：生活費補充", "", "", myTotalUseBadget);

                // 合計の行を追加
                AddListRowData(mySubject, "", "", lngTotalAmount + myTotalUseBadget);
            }
            else
            {
                // 合計の行を追加
                AddListRowData(mySubject, "", "", lngTotalAmount);
            }

            if (mySubject.Equals("収益合計"))
            {
                if (UseBudgetFlag == MainWindow.GRIDV_TOTAL_USEBUDGET_ON)
                    TotalProfit = lngTotalAmount + TotalUseBudget;
                else
                    TotalProfit = lngTotalAmount;
            }
            Regex regex = new Regex("^[3-6][0-9] ");

            if (regex.IsMatch(mySubject))
            {
                TotalExpense += lngTotalAmount;
            }
            //else if (mySubject.Equals("費用合計"))
            //    TotalExpense = lngTotalAmount;
            //else if (mySubject.Equals("貯蓄合計"))
            if (mySubject.Equals("貯蓄合計"))
                TotalSaving = lngTotalAmount;
            else if (mySubject.Equals("予算合計"))
                TotalBadget = lngTotalAmount;

            return;
        }

        private void SetConditionDate()
        {
            string mySqlCommand = "";
            string strFromDate = "";
            string strToDate = "";

            /*
            // グリッドビューから選択されていない場合は最近の給料日を設定
            mySqlCommand = "SELECT CONVERT( varchar(10), MAX(年月日),111) ";
            mySqlCommand = mySqlCommand + "    FROM " + dispinfoTargetTableName;
            mySqlCommand = mySqlCommand + "    WHERE 貸方 = '" + dispinfoTargetSalaryCode + "' ";

            strFromDate = dbcon.getStringSql(mySqlCommand);

            mySqlCommand = "SELECT CONVERT(varchar(10), MAX(年月日), 111) ";
            mySqlCommand = mySqlCommand + "    FROM " + dispinfoTargetTableName;

            strToDate = dbcon.getStringSql(mySqlCommand);

            ConditionFromDate = Convert.ToDateTime(strFromDate);
            ConditionToDate = Convert.ToDateTime(strToDate);
             */

            //本日日付
            DateTime today = DateTime.Today;

            //月初め
            ConditionFromDate = new DateTime(today.Year, today.Month, 1);
            //ConditionFromDate = new DateTime(2020, 1, 1);

            //月終わり
            ConditionToDate = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1);
            //ConditionToDate = new DateTime(2020, 12, 31);
            //ConditionFromDate = new DateTime(2020, 8, 30);
            //ConditionToDate = new DateTime(2020, 8, 31);

        }

        /// <summary>
        /// 集計対象：全て、上位のみのチェックがされた場合
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTargetChanged(object sender, RoutedEventArgs e)
        {
            SetDataSetMakeup();
        }

        private void SetDbTableInfo()
        {
            // フィルタ対象を変更した時に対象とするテーブルを設定する
            if (dispinfoMakeupWayKind == MAKEUPWAY_KIND_SZE)
            {
                dispinfoTargetTableName = "金銭帳SZE";
                dispinfoTargetSalaryCode = "30103";
            }
            else
            {
                dispinfoTargetTableName = "金銭帳";
                dispinfoTargetSalaryCode = TARGET_SALARY_CODE;
            }
        }

        private void OnFilterChanged(object sender, RoutedEventArgs e)
        {
            SetDbTableInfo();

            SetSaralyDate();

            SetDataSetMakeup();
        }

        private void dgridMakeupDetail_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double lgwidth = lgridMoneyInputDetail.ActualWidth;
            Debug.Print("lgridMoneyInputDetail.ActualWidth " + lgwidth);
            double aheight = dgridMakeupDetail.ActualHeight;
            Debug.Print("dgridMakeupDetail.ActualHeight " + aheight);
            double height = dgridMakeupDetail.Height;
            Debug.Print("dgridMakeupDetail.Height " + height);
            double awidth = dgridMakeupDetail.ActualWidth;
            Debug.Print("dgridMakeupDetail.ActualWidth " + awidth);
            double width = dgridMakeupDetail.Width;
            Debug.Print("dgridMakeupDetail.Width " + width);
            //            lgMain.ColumnDefinitions[1].Width = new GridLength(0);
            double abc = lgridMain.RowDefinitions[0].ActualHeight;
            Debug.Print("RowActualHeight " + abc);
        }
        private void OnDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.Print("Start");
            // wpfMovieListMakeプロジェクトの「OnDataGrid_PreviewMouseLeftButtonDown」を参考
            DataGridCell cell = sender as DataGridCell;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                MakeupDetailData selStartData = (MakeupDetailData)dgridMakeupDetail.SelectedItem;

                if (selStartData != null)
                {
                    DataGridRow row = FindVisualParent<DataGridRow>(cell);
                    MakeupDetailData selEndData = row.Item as MakeupDetailData;
                    //Debug.Print("Shiftキーが押されたよ name [" + selStartFile.Name + "] ～ [" + selEndFile.Name + "]");

                    bool selStart = false;
                    foreach (MakeupDetailData data in dgridMakeupDetail.ItemsSource)
                    {
                        if (data.Date.Equals(selStartData.Date)
                            && data.Amount == selStartData.Amount)
                            selStart = true;

                        if (selStart)
                            data.IsSelected = true;

                        if (data.Date.Equals(selEndData.Date)
                            && data.Amount == selEndData.Amount)
                            break;
                    }

                    SetGridMainCount();
                    return;
                }
            }

            // 編集可能なセルの場合のみ実行
            if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
            {
                // フォーカスが無い場合はフォーカスを取得
                if (!cell.IsFocused)
                    cell.Focus();

                DataGrid dataGrid = FindVisualParent<DataGrid>(cell);
                if (dataGrid != null)
                {
                    if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
                    {
                        if (!cell.IsSelected)
                            cell.IsSelected = true;
                    }
                    else
                    {
                        DataGridRow row = FindVisualParent<DataGridRow>(cell);

                        MakeupDetailData selData = row.Item as MakeupDetailData;
                        if (row != null && !row.IsSelected)
                        {
                            if (selData.IsSelected)
                                selData.IsSelected = false;
                            else
                                //row.IsSelected = true;
                                selData.IsSelected = true;
                        }
                        else
                        {
                            if (row.IsSelected && selData.IsSelected)
                                row.IsSelected = false;

                            selData.IsSelected = false;
                        }
                    }
                }
            }

            SetGridMainCount();
        }
        /// <summary>
        /// 選択行とグリッド内の合計値をラベルに表示の更新する
        /// </summary>
        public void SetGridMainCount()
        {
            long SelectTotal = 0;
            long Total = 0;
            foreach (MakeupDetailData data in dgridMakeupDetail.ItemsSource)
            {
                if (data.IsSelected)
                    SelectTotal+=data.Amount;

                Total += data.Amount;
            }

            List<MakeupDetailData> list = (List<MakeupDetailData>)dgridMakeupDetail.ItemsSource;
            int Count = list.Count();

            //lblDetailAmountInfo.Content = SelectTotal.ToString("###,###,###,##0") + "/" + Total.ToString("###,###,###,##0");
        }
        static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private void menuitemSndInput_Click(object sender, RoutedEventArgs e)
        {
            winMoneySzeInput winSzeInput = new winMoneySzeInput();
            winSzeInput.Owner = this;
            //winSzeInput.StartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            winSzeInput.Show();
        }

        private void dgridMakeupDetail_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            Debug.Print("dgridMakeupDetail_BeginningEdit");
        }

        private void btnDetailDelete_Click(object sender, RoutedEventArgs e)
        {
            int delCnt = 0;
            foreach (MakeupDetailData data in dgridMakeupDetail.ItemsSource)
            {
                if (data.IsSelected)
                    delCnt++;
            }

            if (delCnt <= 0)
            {
                MessageBox.Show("削除対象とする行が選択されていません\nDeleteキーか、選択をクリックして削除対象の行を選択して下さい", "削除行未選択");
                return;
            }

            foreach (MakeupDetailData data in dgridMakeupDetail.ItemsSource)
            {
                if (data.IsSelected)
                    OnTargetRowDelete(data);
            }

            // 更新ボタンを有効にする
            OnDetalRowChange();
        }

        private void dgridMakeupDetail_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            DataGridCell cell = e.OldFocus as DataGridCell;
            MakeupDetailData data = dgridMakeupDetail.CurrentCell.Item as MakeupDetailData;
            object obj = dgridMakeupDetail.CurrentItem;

            if (cell == null)
            {
                if (e.OldFocus is TextBox)
                {
                    var txtbox = e.OldFocus as TextBox;
                    cell = txtbox.Parent as DataGridCell;

                    // 各MakeupDetailDataのメソッドで入力値をチェックしてプロパティへ格納
                    // 行変更のためのOperateも更新する
                    if (cell.Column.Header.Equals("借CD"))
                        data.SetDebitCodeFromGridTextbox(txtbox.Text);
                    else if (cell.Column.Header.Equals("貸CD"))
                        data.SetCreditCodeFromGridTextbox(txtbox.Text);
                    else if (cell.Column.Header.Equals("年月日"))
                        txtbox.Text = data.SetDateFromGridTextbox(txtbox.Text);
                    else if (cell.Column.Header.Equals("金額"))
                        txtbox.Text = Convert.ToString(data.SetAmountFromGridTextbox(txtbox.Text));
                    else if (cell.Column.Header.Equals("摘要"))
                    {
                        if (!data.Remark.Equals(txtbox.Text))
                        {
                            data.Remark = txtbox.Text;
                            data.Operate = 1;
                        }
                    }
                }

                if (cell == null)
                {
                    // 何もしない、後続のelse ifを実行させない
                    // Debug.Print("行の新規作成");
                    dispinfoBeforeGridRowId = data.Id;
                    return;
                }
                if (cell.Column.Header.Equals("借CD"))
                {
                    if (data.DebitCode == null || data.DebitCode.Length <= 0)
                    {
                        data.DebitCode = Account.CODE_CASH;
                        data.DebitName = account.getName(Account.CODE_CASH);
                    }
                    else
                        data.DebitName = account.getName(data.DebitCode);
                }
                else if (cell.Column.Header.Equals("貸CD"))
                {
                    if (data.CreditCode == null || data.CreditCode.Length <= 0)
                    {
                        data.CreditCode = Account.CODE_CASH;
                        data.CreditName = account.getName(Account.CODE_CASH);
                    }
                    else
                        data.CreditName = account.getName(data.CreditCode);
                }

                if (data != null && data.Operate != 0)
                    // 更新ボタンを有効にする
                    OnDetalRowChange();

                dispinfoBeforeGridRowId = data.Id;
            }
        }

        /// <summary>
        /// DELキーが押された場合は行のステータスを削除に変更する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgridMakeupDetail_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                bool change = false;
                var grid = (DataGrid)sender;

                if (grid.SelectedItems.Count > 0)
                {
                    foreach (MakeupDetailData data in dgridMakeupDetail.SelectedItems)
                    {
                        OnTargetRowDelete(data);
                        change = true;
                    }
                }

                if (change)
                    // 更新ボタンを有効にする
                    OnDetalRowChange();
            }
        }

        /// <summary>
        /// 行を削除ステータスにする
        /// ※ 行が既に削除ステータスの場合は元に戻す
        /// </summary>
        /// <param name="myData"></param>
        private void OnTargetRowDelete(MakeupDetailData myData)
        {
            if (myData.Operate == 2)
                myData.Operate = 0;
            else
                myData.Operate = 2;

            myData.IsSelected = false;
        }

        private void OnDetalRowChange()
        {
            // 行の変更、削除操作がされて行の色が変わったらボタンを有効にする
            btnDetailUpdate.IsEnabled = true;
        }

        private void btnDetailUpdate_Click(object sender, RoutedEventArgs e)
        {
            string TableName = "";
            if (dispinfoMakeupWayKind == MAKEUPWAY_KIND_SZE)
                TableName = "金銭帳SZE";
            else
                TableName = "金銭帳";

            List<MakeupDetailData> list = (List<MakeupDetailData>)dgridMakeupDetail.ItemsSource;

            int rowUpdate = 0, rowDelete = 0;
            foreach (MakeupDetailData data in list)
            {
                if (data.Operate == 0)
                    continue;

                if (data.Operate == 1)
                    rowUpdate++;
                else
                    rowDelete++;
            }

            if (rowUpdate > 0 && rowDelete > 0)
            {
                MessageBoxResult res = MessageBox.Show(this, String.Format("変更行が{0}件、削除行が{1}件、更新・削除を反映して宜しいですか？", rowUpdate, rowDelete), "画面更新・削除最終確認", MessageBoxButton.OKCancel);

                if (res == MessageBoxResult.Cancel)
                    return;
            }
            else if (rowUpdate > 0)
            {
                MessageBoxResult res = MessageBox.Show(this, String.Format("変更行が{0}件、更新して宜しいですか？", rowUpdate), "画面更新最終確認", MessageBoxButton.OKCancel);

                if (res == MessageBoxResult.Cancel)
                    return;
            }
            else if (rowDelete > 0)
            {
                MessageBoxResult res = MessageBox.Show(this, String.Format("削除行が{0}件、削除して宜しいですか？", rowDelete), "画面削除最終確認", MessageBoxButton.OKCancel);

                if (res == MessageBoxResult.Cancel)
                    return;
            }
            else
            {
                MessageBox.Show("更新・削除を実行する行が存在しません");
            }

            // データベース：トランザクションを開始
            dbcon.BeginTransaction("MONEYINPUTBEGIN");

            try
            {
                foreach (MakeupDetailData data in list)
                {
                    if (data.Operate == 0)
                        continue;

                    if (data.Operate == 1)
                    {
                        Debug.Print("更新 ID [" + data.Id + "]");
                        MoneyInput.UpdateDbMakeupDetailData(data, TableName, dbcon);
                    }
                    else
                    {
                        Debug.Print("削除 ID [" + data.Id + "]");
                        MoneyInput.DeleteDbMakeupDetailData(data, TableName, dbcon);
                    }
                    data.Operate = 0;
                }
            }
            catch (SqlException sqlex)
            {
                Debug.Write(sqlex);
                MessageBox.Show("SqlException発生 " + sqlex.Message);
                dbcon.RollbackTransaction();
                return;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("btnDetailUpdate_Click ", ex);
                Debug.Write(ex);
                MessageBox.Show("Exception発生 " + ex.Message);
                dbcon.RollbackTransaction();
                return;
            }
            dbcon.CommitTransaction();

            btnDetailUpdate.IsEnabled = false;
        }

        private void menuitemAddNextMonth_Click(object sender, RoutedEventArgs e)
        {
            MakeupDetailData data = (MakeupDetailData)dgridMakeupDetail.SelectedItem;

            DateTime dt = data.Date;
            DateTime monthPlusDt = dt.AddMonths(1);
            calendarNextMonthDate.DisplayDate = monthPlusDt;
            calendarNextMonthDate.SelectedDate = monthPlusDt;

            //labelNextMonthCodeInfo.Text = data.Date.ToString() + " " + data.DebitCode + " " + data.DebitName + " " + data.CreditCode + " " + data.CreditName;
            labelNextMonth.Text = data.Date.ToString("yyyy/MM/dd");
            txtNextMonthDebitCode.Text = data.DebitCode;
            labelNextMonthDebitName.Text = data.DebitName;
            txtNextMonthCreditCode.Text = data.CreditCode;
            labelNextMonthCreditName.Text = data.CreditName;
            txtNextMonthAmount.Text = data.Amount.ToString();
            txtNextMonthRemark.Text = data.Remark;

            lgridNextMonth.Visibility = System.Windows.Visibility.Visible;
        }

        private void btnNextMonthCancel_Click(object sender, RoutedEventArgs e)
        {
            lgridNextMonth.Visibility = System.Windows.Visibility.Hidden;
        }

        private void btnNextMonthRegist_Click(object sender, RoutedEventArgs e)
        {
            // データベース：トランザクションを開始
            dbcon.BeginTransaction("MONEYINPUTBEGIN");

            string TableName = "";
            if (dispinfoMakeupWayKind == MAKEUPWAY_KIND_SZE)
                TableName = "金銭帳SZE";
            else
                TableName = "金銭帳";

            try
            {
                MakeupDetailData data = new MakeupDetailData();

                data.Date = Convert.ToDateTime(calendarNextMonthDate.SelectedDate);
                data.DebitCode = txtNextMonthDebitCode.Text;
                data.CreditCode = txtNextMonthCreditCode.Text;
                data.Amount = Convert.ToInt64(txtNextMonthAmount.Text);
                data.Remark = txtNextMonthRemark.Text;

                MoneyInput.InsertDbMakeupDetailData(data, TableName, dbcon);
            }
            catch (SqlException sqlex)
            {
                Debug.Write(sqlex);
                MessageBox.Show("SqlException発生 " + sqlex.Message);
                dbcon.RollbackTransaction();
                return;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("btnRegist_Click ", ex);
                Debug.Write(ex);
                MessageBox.Show("Exception発生 " + ex.Message);
                dbcon.RollbackTransaction();
                return;
            }
            dbcon.CommitTransaction();

            lgridNextMonth.Visibility = System.Windows.Visibility.Hidden;
        }

        private void menuitemSetDateThisYear_Click(object sender, RoutedEventArgs e)
        {
            SetYear2ConditionDate(DateTime.Now.Year);
            
            SetDataSetMakeup();
        }

        private void menuitemSetDateLastYear_Click(object sender, RoutedEventArgs e)
        {
            SetYear2ConditionDate(DateTime.Now.Year - 1);

            SetDataSetMakeup();
        }

        /// <summary>
        /// コンテキストメニューで指定された年から期間を設定する
        /// </summary>
        /// <param name="myYear"></param>
        private void SetYear2ConditionDate(int myYear)
        {
            string mySqlCommand = "";
            string strFromDate = "";
            string strToDate = "";

            // グリッドビューから選択されていない場合は最近の給料日を設定
            mySqlCommand = "SELECT CONVERT( varchar(10), MIN(年月日),111) ";
            mySqlCommand = mySqlCommand + "    FROM " + dispinfoTargetTableName;
            mySqlCommand = mySqlCommand + "    WHERE 貸方 = '" + dispinfoTargetSalaryCode + "' ";
            mySqlCommand = mySqlCommand + "      AND (年月日 >='" + myYear + "/1/1' AND 年月日 <= '" + myYear + "/12/31')";

            strFromDate = dbcon.getStringSql(mySqlCommand);

            if (strFromDate.Equals("Null"))
                strFromDate = myYear + "/1/1";

            mySqlCommand = "SELECT CONVERT(varchar(10), MAX(年月日), 111) ";
            mySqlCommand = mySqlCommand + "    FROM " + dispinfoTargetTableName;
            mySqlCommand = mySqlCommand + "    WHERE 年月日 >='" + myYear + "/1/1' AND 年月日 <= '" + myYear + "/12/31'";

            strToDate = dbcon.getStringSql(mySqlCommand);

            ConditionFromDate = Convert.ToDateTime(strFromDate);
            ConditionToDate = Convert.ToDateTime(strToDate);

            txtbTermDate.Text = ConditionFromDate.ToString("yyyy/MM/dd") + " ～ " + ConditionToDate.ToString("yyyy/MM/dd");

            return;
        }

        private void menuitemInput_Click(object sender, RoutedEventArgs e)
        {
            winMoneyInput win = new winMoneyInput();
            win.Owner = this;
            win.Show();
        }

        private void menuitemArrear_Click(object sender, RoutedEventArgs e)
        {

            winArrear win = new winArrear();
            win.Owner = this;
            win.Show();
        }

        private void menuitemAfterwordsPayment_Click(object sender, RoutedEventArgs e)
        {

            winAfterwordsPayment win = new winAfterwordsPayment();
            win.Owner = this;
            win.Show();
        }

        private void dgridPaymentSchedule_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                MessageBoxResult result = MessageBox.Show("削除して宜しいですか？", "削除確認", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.Cancel)
                    return;

                DbConnection dbcon = new DbConnection();
                dbcon.BeginTransaction("ROWS_DELETE");

                if (dgridPaymentSchedule.SelectedItems.Count > 1)
                {
                    foreach (PaymentData data in dgridPaymentSchedule.SelectedItems)
                    {
                        data.DbDeletePaymentDecision(dbcon);
                        listPaymentDecision.Remove(data);
                    }
                }
                else if (dgridPaymentSchedule.SelectedItems.Count == 1)
                {
                    PaymentData seldata = (PaymentData)dgridPaymentSchedule.SelectedItem;
                    seldata.DbDeletePaymentDecision(dbcon);
                    listPaymentDecision.Remove(seldata);
                }

                dbcon.CommitTransaction();

                dgridPaymentSchedule.Items.Refresh();

                // 支払確定の行の色を設定
                SetRowColorPaymentDesicion();
            }

        }

        private void dgridPaymentSchedule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selcount = 0;
            long total = 0;
            List<PaymentData> listseldata = new List<PaymentData>();
            if (dgridPaymentSchedule.SelectedItems.Count > 1)
            {
                foreach (PaymentData data in dgridPaymentSchedule.SelectedItems)
                {
                    listseldata.Add(data);
                    selcount++;
                }
            }
            else
            {
                PaymentData data = (PaymentData)dgridPaymentSchedule.SelectedItem;
                listseldata.Add(data);
                selcount = 1;
            }

            foreach (PaymentData data in listseldata)
            {
                if (data == null)
                    continue;

                total += data.Amount;
            }

            txtbPaymentScheduleSelectAmount.Text = "選択 " + String.Format("{0:###,###,##0}", total);
        }

        private void cmbMakeupTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                dispinfoMakeupWayKind = Convert.ToInt32(cmbMakeupTarget.SelectedValue);

                OnFilterChanged(null, null);

                SetConditionDate();
            }
            catch (Exception)
            {
                dispinfoMakeupWayKind = 0;
            }
            OnFilterChanged(null, null);
        }

        private void menuitemUpperOnly_Click(object sender, RoutedEventArgs e)
        {
            dispinfoMakeupWayUpperOnly = menuitemUpperOnly.IsChecked;

            if (dispinfoMakeupWayUpperOnly)
                dispinfoMakeupWayUnionKind = ST_DO_UNION_UPPERONLY;
            else
                dispinfoMakeupWayUnionKind = ST_DO_UNION_ALL;

            SetDataSetMakeup();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // DoubleClick
            if (e.ClickCount == 2 && e.LeftButton == MouseButtonState.Pressed)
            {
                lborderSetMakeupScopeDate.Visibility = System.Windows.Visibility.Visible;

                dispctrlMakeupScopeDate[0] = ConditionFromDate;
                dispctrlMakeupScopeDate[1] = ConditionToDate;

                txtbMakeScopeDate.Text = MakeupCalcurate.GetMakeupScopeDisplayDate(dispctrlMakeupScopeDate);
            }
        }

        private void OnToggleButtonMakeupDetailFilterClick(object sender, RoutedEventArgs e)
        {
            ToggleButton tbtnsender = sender as ToggleButton;

            string tbtnLabel = tbtnsender.Content.ToString();

            List<ToggleButton> listtbtn = CommonMethod.FindVisualChild<ToggleButton>(lstackSearchFilter, "ToggleButton");

            dispinfoMakeupDetailFilterButton = new bool[6];

            if (tbtnLabel.Equals("全て"))
            {
                dispinfoMakeupDetailFilterButton[0] = true;
                dispinfoMakeupDetailFilterButton[1] = true;
                dispinfoMakeupDetailFilterButton[2] = true;
                dispinfoMakeupDetailFilterButton[3] = true;
                dispinfoMakeupDetailFilterButton[4] = true;
                dispinfoMakeupDetailFilterButton[5] = true;
                foreach (ToggleButton tbtn in listtbtn)
                {
                    if (tbtn.Content.ToString().Equals("全て"))
                        continue;

                    tbtn.IsChecked = false;
                }
            }
            else
            {
                foreach (ToggleButton tbtn in listtbtn)
                {
                    if (tbtn.Equals("全て"))
                    {
                        tbtn.IsChecked = false;
                        continue;
                    }

                    bool chk = Convert.ToBoolean(tbtn.IsChecked);
                    if (tbtn.Content.ToString().Equals("MY"))
                    {
                        if (tbtn.Content.ToString().Equals("MY") && chk)
                            dispinfoMakeupDetailFilterButton[0] = true;
                        else
                            dispinfoMakeupDetailFilterButton[0] = false;
                        continue;
                    }

                    if (tbtn.Content.ToString().Equals("カード"))
                    {
                        if (tbtn.Content.ToString().Equals("カード") && chk)
                            dispinfoMakeupDetailFilterButton[1] = true;
                        else
                            dispinfoMakeupDetailFilterButton[1] = false;
                        continue;
                    }

                    if (tbtn.Content.ToString().Equals("SZE"))
                    {
                        if (tbtn.Content.ToString().Equals("SZE") && chk)
                            dispinfoMakeupDetailFilterButton[2] = true;
                        else
                            dispinfoMakeupDetailFilterButton[2] = false;
                        continue;
                    }

                    if (tbtn.Content.ToString().Equals("会社"))
                    {
                        if (tbtn.Content.ToString().Equals("会社") && chk)
                            dispinfoMakeupDetailFilterButton[3] = true;
                        else
                            dispinfoMakeupDetailFilterButton[3] = false;
                        continue;
                    }
                    if (tbtn.Content.ToString().Equals("会社未払"))
                    {
                        if (tbtn.Content.ToString().Equals("会社未払") && chk)
                            dispinfoMakeupDetailFilterButton[4] = true;
                        else
                            dispinfoMakeupDetailFilterButton[4] = false;
                        continue;
                    }
                    if (tbtn.Content.ToString().Equals("妻未払"))
                    {
                        if (tbtn.Content.ToString().Equals("妻未払") && chk)
                            dispinfoMakeupDetailFilterButton[5] = true;
                        else
                            dispinfoMakeupDetailFilterButton[5] = false;
                        continue;
                    }
                }

                if (dispinfoMakeupDetailFilterButton[0] 
                    && dispinfoMakeupDetailFilterButton[1]
                    && dispinfoMakeupDetailFilterButton[2]
                    && dispinfoMakeupDetailFilterButton[3]
                    && dispinfoMakeupDetailFilterButton[4]
                    && dispinfoMakeupDetailFilterButton[5])
                {
                    foreach (ToggleButton tbtn in listtbtn)
                    {
                        if (tbtn.Content.ToString().Equals("全て"))
                        {
                            tbtn.IsChecked = true;
                            continue;
                        }

                        tbtn.IsChecked = false;
                    }
                }
                else if (!dispinfoMakeupDetailFilterButton[0]
                    && !dispinfoMakeupDetailFilterButton[1]
                    && !dispinfoMakeupDetailFilterButton[2]
                    && !dispinfoMakeupDetailFilterButton[3]
                    && !dispinfoMakeupDetailFilterButton[4]
                    && !dispinfoMakeupDetailFilterButton[5])
                {
                    foreach (ToggleButton tbtn in listtbtn)
                    {
                        if (tbtn.Content.ToString().Equals("全て"))
                        {
                            tbtn.IsChecked = true;
                            break;
                        }
                    }
                    dispinfoMakeupDetailFilterButton[0] = true;
                    dispinfoMakeupDetailFilterButton[1] = true;
                    dispinfoMakeupDetailFilterButton[2] = true;
                    dispinfoMakeupDetailFilterButton[3] = true;
                    dispinfoMakeupDetailFilterButton[4] = true;
                    dispinfoMakeupDetailFilterButton[5] = true;
                }
                else
                {
                    foreach (ToggleButton tbtn in listtbtn)
                    {
                        if (tbtn.Content.ToString().Equals("全て"))
                        {
                            tbtn.IsChecked = false;
                            break;
                        }
                    }
                }
            }

            if (dispinfoMakeupDetailFilterButton[4] == true)
                dispctrlMakeupDetailMode = MAKEUPDETAIL_MODE_SEARCH_ARREAR;
            else
                dispctrlMakeupDetailMode = MAKEUPDETAIL_MODE_SEARCH;

            SetViewFilterAndSort();
        }

        private void OnToggleButtonMakeupScopeDateClick(object sender, RoutedEventArgs e)
        {
            ToggleButton tbtn = sender as ToggleButton;

            if (dgridSelectDate.SelectedItem == null)
                return;

            DateTime dt = (DateTime)dgridSelectDate.SelectedItem;

            if (tbtn.Content.Equals("FROM"))
            {
                if (tbtn.IsChecked.Value)
                    dispctrlMakeupScopeDate[0] = null;
            }
            else
            {
                if (tbtn.IsChecked.Value)
                    dispctrlMakeupScopeDate[1] = null;
            }
            //txtbMakeScopeDate.Text = MakeupCalcurate.GetMakeupScopeDisplayDate(dispctrlMakeupScopeDate);
        }

        private void cmbSetMakeupScopeDateYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSetMakeupScopeDateYear.SelectedItem == null)
                return;

            int year = (int)cmbSetMakeupScopeDateYear.SelectedItem;

            if (radioMakeScopeDateFiscalYear.IsChecked.Value)
            {
                dispctrlMakeupScopeDate = MakeupCalcurate.GetMakeScopeDateFiscalYear(year, listMakeupScopeDate);

                if (MakeupCalcurate.IsMakeupScopeDateValidValue(dispctrlMakeupScopeDate))
                    cmbSetMakeupScopeDateYear.SelectedItem = null;
            }
            else
            {
                dispctrlMakeupScopeDate = MakeupCalcurate.GetMakeScopeDateYear(year, listMakeupScopeDate);

                if (MakeupCalcurate.IsMakeupScopeDateValidValue(dispctrlMakeupScopeDate))
                    cmbSetMakeupScopeDateYear.SelectedItem = null;
            }

            txtbMakeScopeDate.Text = MakeupCalcurate.GetMakeupScopeDisplayDate(dispctrlMakeupScopeDate);
            dgridSelectDate.SelectedItem = null;
            //Debug.Print("YEAR " + dtFrom.ToString("yyyy/MM/dd") + "～" + dtTo.ToString("yyyy/MM/dd"));
        }

        private void dgridSelectDate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dispctrlMakeupScopeDate[0] == null || dispctrlMakeupScopeDate[1] == null)
            {
                if (dispctrlMakeupScopeDate[0] == null)
                    dispctrlMakeupScopeDate[0] = (DateTime)dgridSelectDate.SelectedItem;
                if (dispctrlMakeupScopeDate[1] == null)
                    dispctrlMakeupScopeDate[1] = (DateTime)dgridSelectDate.SelectedItem;

                List<ToggleButton> list = CommonMethod.FindVisualChild<ToggleButton>(lgridMakeScopeDateControl, "ToggleButton");

                foreach (ToggleButton tbtn in list)
                    tbtn.IsChecked = false;

                txtbMakeScopeDate.Text = MakeupCalcurate.GetMakeupScopeDisplayDate(dispctrlMakeupScopeDate);

                return;
            }

            cmbSetMakeupScopeDateYear.SelectedItem = null;
            DateTime selDt = (DateTime)dgridSelectDate.SelectedItem;
            DateTime beforeDt = new DateTime(1900, 1, 1);
            foreach (DateTime dt in listMakeupScopeDate)
            {
                if (selDt.CompareTo(dt) == 0)
                    break;

                beforeDt = dt.AddDays(-1);
            }

            // 一番先頭の場合はTO日付には1ヶ月後を設定
            if (beforeDt.Year == 1900)
                beforeDt = selDt.AddMonths(1);

            txtbMakeScopeDate.Text = selDt.ToString("yyyy/MM/dd") + "～" + beforeDt.ToString("yyyy/MM/dd");
        }

        private void btnMakeupScopeDateExecuteSet_Click(object sender, RoutedEventArgs e)
        {
            ConditionFromDate = Convert.ToDateTime(dispctrlMakeupScopeDate[0]);
            ConditionToDate = Convert.ToDateTime(dispctrlMakeupScopeDate[1]);

            lborderSetMakeupScopeDate.Visibility = System.Windows.Visibility.Hidden;

            SetDataSetMakeup();

            //txtbTermDate.Text = ConditionFromDate.ToString("yyyy/MM/dd") + " ～ " + ConditionToDate.ToString("yyyy/MM/dd");
        }

        private void OnButtonMakeupScopeDateChangeMonth(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            DateTime selDt = ConditionFromDate;
            DateTime matchDt = new DateTime(1900, 1, 1);

            if (btn.Content.Equals("＜"))
            {
                if (dispinfoMakeupWayKind == MAKEUPWAY_KIND_SZE)
                {
                    dispctrlMakeupScopeDate[0] = selDt.AddMonths(-1);
                    dispctrlMakeupScopeDate[1] = dispctrlMakeupScopeDate[0].Value.AddMonths(1).AddDays(-1);
                }
                else
                {
                    bool IsMatch = false;
                    foreach (DateTime dt in listMakeupScopeDate)
                    {
                        if (IsMatch)
                        {
                            matchDt = dt;
                            break;
                        }

                        if (selDt.CompareTo(dt) == 0)
                            IsMatch = true;
                    }
                    dispctrlMakeupScopeDate[0] = matchDt;
                    dispctrlMakeupScopeDate[1] = selDt.AddDays(-1);
                }

                dispctrlMakeupScopeDate[0] = ConditionFromDate.AddMonths(-1);
                dispctrlMakeupScopeDate[1] = dispctrlMakeupScopeDate[0].Value.AddMonths(1).AddDays(-1);
            }
            else
            {
                if (dispinfoMakeupWayKind == MAKEUPWAY_KIND_SZE)
                {
                    dispctrlMakeupScopeDate[0] = selDt.AddMonths(1);
                    dispctrlMakeupScopeDate[1] = dispctrlMakeupScopeDate[0].Value.AddMonths(1).AddDays(-1);
                }
                else
                {
                    foreach (DateTime dt in listMakeupScopeDate)
                    {
                        if (selDt.CompareTo(dt) == 0)
                            break;

                        matchDt = dt;
                    }
                    dispctrlMakeupScopeDate[0] = matchDt;
                    foreach (DateTime dt in listMakeupScopeDate)
                    {
                        if (selDt.CompareTo(dt) == 0)
                        {
                            if (matchDt.CompareTo(dispctrlMakeupScopeDate[0].Value) == 0)
                                matchDt = dispctrlMakeupScopeDate[0].Value.AddMonths(1);
                            else
                                matchDt = matchDt.AddDays(-1);
                            break;
                        }

                        matchDt = dt;
                    }

                    dispctrlMakeupScopeDate[1] = matchDt.AddDays(-1);
                }
                dispctrlMakeupScopeDate[0] = ConditionFromDate.AddMonths(1);
                dispctrlMakeupScopeDate[1] = dispctrlMakeupScopeDate[0].Value.AddMonths(1).AddDays(-1);
            }

            /*
            if (dispinfoMakeupWayKind != MAKEUPWAY_KIND_SZE)
            {
                if (matchDt.Year == 1900)
                    return;
            }
             */

            ConditionFromDate = dispctrlMakeupScopeDate[0].Value;
            ConditionToDate = dispctrlMakeupScopeDate[1].Value;

            txtbTermDate.Text = MakeupCalcurate.GetMakeupScopeDisplayDate(dispctrlMakeupScopeDate);

            SetDataSetMakeup();

            dispctrlMakeupDetailMode = MAKEUPDETAIL_MODE_ACCOUNTPAYMENT;
            SwitchLayout(LAYOUTMODE_MONEYINPUTDETAIL);

            txtbMakeupDetailLoanInfo.Text = "";

            dgridMakeupDetail.IsReadOnly = true;
        }

        private void btnSearchExecute_Click(object sender, RoutedEventArgs e)
        {
            Regex regDateMonth = new Regex("[2][0][0-2][0-9][0-1][0-9]");
            Regex regNum = new Regex("\\d+");

            dispinfoSearchText = txtSearch.Text;
            if (dispinfoSearchText == null || dispinfoSearchText.Length <= 0)
                return;

            dispctrlMakeupDetailMode = MAKEUPDETAIL_MODE_SEARCH;
            SwitchLayout(LAYOUTMODE_SEARCH);
            //dgridMakeupDetail_SizeChanged(null, null);
        }

        private void dgridMakeupDetail_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dispinfoSelectDataGridMakeupDetailData = (MakeupDetailData)dgridMakeupDetail.SelectedItem;

            string kind = account.getAccountKind(dispinfoSelectDataGridMakeupDetailData.DebitCode);

            if (kind.Equals(Account.KIND_DEPT_LOAN))
            {
                dispctrlMakeupDetailMode = MAKEUPDETAIL_MODE_LOANDETAIL;
                SwitchLayout(LAYOUTMODE_MONEYINPUTDETAIL);
            }
        }

        private void btnMakeupDetailBack_Click(object sender, RoutedEventArgs e)
        {
            dispctrlMakeupDetailMode = MAKEUPDETAIL_MODE_ACCOUNTPAYMENT;
            SwitchLayout(LAYOUTMODE_MONEYINPUTDETAIL);
        }

        private void lgridMoneyInputDetail_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DataGridMakeupDetailWidthSetting();
        }

        private void dgridMakeupDetail_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            long total = 0;
            int selcount = 0;
            if (dgridMakeupDetail.SelectedItems.Count > 1)
            {
                foreach (MakeupDetailData data in dgridMakeupDetail.SelectedItems)
                {
                    total += data.Amount;
                    selcount++;
                }
            }
            else if (dgridMakeupDetail.SelectedItems.Count == 1)
            {
                MakeupDetailData seldata = (MakeupDetailData)dgridMakeupDetail.SelectedItem;
                total = seldata.Amount;
                selcount = 1;
            }

            txtbMakeupDetailSelectInfo.Text = "選択 " + selcount + "件 " + String.Format("{0:###,###,##0}", total);
        }

        private void menuitemTravelRegist_Click(object sender, RoutedEventArgs e)
        {
            lborderSetTravel.Visibility = System.Windows.Visibility.Visible;

            DbConnection dbcon = new DbConnection();

            dispinfoInputTravelData = new TravelData();
            dispinfoInputTravelData.SetNewCode(dbcon);

            txtTravelRegistCode.Text = dispinfoInputTravelData.Code;
        }

        private void btnTravelRegist_Click(object sender, RoutedEventArgs e)
        {
            lborderSetTravel.Visibility = System.Windows.Visibility.Hidden;

            dispinfoInputTravelData.Name = txtTravelRegistName.Text;
            dispinfoInputTravelData.DepartureDate = Convert.ToDateTime(dtpickerDepartureDate.SelectedDate);
            dispinfoInputTravelData.ArrivalDate = Convert.ToDateTime(dtpickerArrivalDate.SelectedDate);
            dispinfoInputTravelData.Detail = txtTravelRegistDetail.Text;
            dispinfoInputTravelData.Remark = txtTravelRegistRemark.Text;

            DbConnection dbcon = new DbConnection();

            dbcon.BeginTransaction("TRAVEL_REGIST");

            dispinfoInputTravelData.DbExport(dbcon);

            AccountData account = new AccountData();

            account.Code = dispinfoInputTravelData.Code;
            account.Name = dispinfoInputTravelData.Name;
            account.Kind = Account.KIND_TRAVEL;
            account.UpperCode = "70000";

            account.DbExport(dbcon);

            dbcon.CommitTransaction();

            txtTravelRegistCode.Text = "";
            txtTravelRegistName.Text = "";
            dtpickerDepartureDate.SelectedDate = null;
            txtTravelRegistDetail.Text = "";
            dtpickerArrivalDate.SelectedDate = null;
            txtTravelRegistRemark.Text = "";
            dispinfoInputTravelData = null;
        }

        private void menuitemCsvOut_Click(object sender, RoutedEventArgs e)
        {
            lborderCsvOut.Visibility = System.Windows.Visibility.Visible;
            ColViewListInputDataDetail.Filter = null;

            datepCsvOutFrom.Text = "2020/06/01";
            datepCsvOutTo.Text = "2021/05/31";
            //datepCsvOutFrom.Text = "2018/07/02";
            //datepCsvOutTo.Text = "2018/07/02";
        }

        private void btnCsvOutExecute_Click(object sender, RoutedEventArgs e)
        {
            CsvOut csvOut = new CsvOut(account);
            Arrears arrears = new Arrears();

            DateTime from = Convert.ToDateTime(datepCsvOutFrom.SelectedDate);
            DateTime to = Convert.ToDateTime(datepCsvOutTo.SelectedDate);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Public\test.csv", false, System.Text.Encoding.GetEncoding("shift_jis")))
            {
                ColViewListInputDataDetail.Filter = delegate(object o)
                {
                    MakeupDetailData data = o as MakeupDetailData;

                    //Debug.Print("dispinfoFilterCreditCode [" + dispinfoFilterCreditCode + "]    data.CreditCode [" + data.CreditCode + "]");
                    bool IsMatch = false;
                    if (data.Date.CompareTo(from) >= 0
                        && data.Date.CompareTo(to) <= 0)
                    {
                        if (data.IsCompany() && data.UsedCompanyArrear == 0)
                            file.WriteLine(csvOut.GetCsvData(data));
                    }

                    return IsMatch;
                };

                List<Arrear> listArrear = arrears.GetRangeDate(from, to, null);

                foreach (Arrear arrear in listArrear)
                {
                    file.WriteLine(csvOut.GetCsvDataArrear(arrear));
                }
            }

            lborderCsvOut.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void menuitemAddCompanyGoudouArrear_Click(object sender, RoutedEventArgs e)
        {
            dbcon.openConnection();

            List<MakeupDetailData> listDetail = GetDataGridMakeupDetail();

            Arrears arrears = new Arrears();

            arrears.Regist(listDetail, 2, dbcon);

            // 正常にキャンセル処理が終了した場合はDataGrid上のデータを更新する
            UpdateDataGridCompanyArrear(1);
        }

        private void menuitemAddCompanyArrearCancel_Click(object sender, RoutedEventArgs e)
        {
            dbcon.openConnection();

            List<MakeupDetailData> listDetail = GetDataGridMakeupDetail();

            Arrears arrears = new Arrears();

            arrears.Cancel(listDetail, dbcon);

            // 正常にキャンセル処理が終了した場合はDataGrid上のデータを更新する
            UpdateDataGridCompanyArrear(0);
        }

        private List<MakeupDetailData> UpdateDataGridCompanyArrear(int myUsedCompanyArrear)
        {
            List<MakeupDetailData> listDetail = new List<MakeupDetailData>();

            if (dgridMakeupDetail.SelectedItems.Count == 1)
            {
                MakeupDetailData data = (MakeupDetailData)dgridMakeupDetail.SelectedItem;
                data.UsedCompanyArrear = myUsedCompanyArrear;
            }
            else if (dgridMakeupDetail.SelectedItems.Count > 1)
            {
                foreach (MakeupDetailData data in dgridMakeupDetail.SelectedItems)
                    data.UsedCompanyArrear = myUsedCompanyArrear;
            }

            return listDetail;
        }

        private List<MakeupDetailData> GetDataGridMakeupDetail()
        {
            List<MakeupDetailData> listDetail = new List<MakeupDetailData>();

            if (dgridMakeupDetail.SelectedItems.Count == 1)
            {
                MakeupDetailData data = (MakeupDetailData)dgridMakeupDetail.SelectedItem;
                listDetail.Add(data);
            }
            else if (dgridMakeupDetail.SelectedItems.Count > 1)
            {
                foreach (MakeupDetailData data in dgridMakeupDetail.SelectedItems)
                    listDetail.Add(data);
            }

            return listDetail;
        }

        /// <summary>
        /// コンテキストメニューを開く前に会社未払のデータのチェックを行い、既に会社未払の場合はキャンセルのメニューを表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuitemAddCompanyArrear_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            DataGrid datagrid = sender as DataGrid;
            // *wpfを再生のメニューを削除してクリアする
            string[] arrMenuItemName = { "会社未払へ算入", "会社未払へ算入をキャンセル" };
            bool[] isExistMenu = new bool[arrMenuItemName.Length];
            bool[] isNeedMenu = new bool[arrMenuItemName.Length];

            dbcon.openConnection();

            List<MakeupDetailData> listDetail = GetDataGridMakeupDetail();

            // コンテキストメニューに存在するかのチェック
            //   複数回右クリックをすると都度メニューが追加・削除されるのでチェックを行う
            int idx = 0;
            foreach (string menuName in arrMenuItemName)
            {
                isExistMenu[idx] = false;
                foreach (var anyObject in datagrid.ContextMenu.Items)
                {
                    System.Type any = anyObject.GetType();
                    MenuItem menuitem = anyObject as MenuItem;
                    if (menuitem.Header.ToString().Equals(menuName))
                    {
                        isExistMenu[idx] = true;
                        break;
                    }
                }
                idx++;
            }

            // コンテキストメニューに追加する対象のデータかをチェック
            //   既に会社未払の場合はキャンセルのメニュー、通常のデータの場合は算入のメニュー
            isNeedMenu[0] = false;
            isNeedMenu[1] = false;
            foreach (MakeupDetailData data in listDetail)
            {
                MakeupDetailData findData = null;

                if (data.Id <= 0)
                    // カードの明細の場合は金銭帳データを取得
                    findData = MoneyInput.GetData(data, dbcon);
                else
                    findData = data;

                //if (findData == null)
                //    throw new Exception("カード明細に一致する金銭帳のデータが存在しません");

                if (data.UsedCompanyArrear <= 0)
                    isNeedMenu[0] = true;

                if (data.UsedCompanyArrear > 0)
                    isNeedMenu[1] = true;

            }

            // 前の２つのループで取得したisExistMenu, isNeedMenuでコンテキストメニューを追加・削除する
            idx = 0;
            foreach (string menuName in arrMenuItemName)
            {
                // 削除
                if (isExistMenu[idx] && !isNeedMenu[idx])
                {
                    foreach (MenuItem menuitem in datagrid.ContextMenu.Items)
                    {
                        if (menuitem.Header.ToString().Equals(menuName))
                        {
                            datagrid.ContextMenu.Items.Remove(menuitem);
                            break;
                        }
                    }
                }
                // 追加
                else if (!isExistMenu[idx] && isNeedMenu[idx])
                {
                    MenuItem menuitem = new MenuItem();
                    menuitem.Header = menuName;
                    if (idx == 0)
                        menuitem.Click += menuitemAddCompanyGoudouArrear_Click;
                    else if (idx == 1)
                        menuitem.Click += menuitemAddCompanyArrearCancel_Click;

                    datagrid.ContextMenu.Items.Add(menuitem);
                }
                idx++;
            }
        }
        private void menuitemCancelRegist_Click(object sender, RoutedEventArgs e)
        {
            lborderCancelRegist.Visibility = System.Windows.Visibility.Visible;

            txtbCancelRegistDate.Text = ConditionToDate.ToString("yyyy/MM/dd");
        }

        private void lborderCancelRegist_MouseUp(object sender, MouseButtonEventArgs e)
        {
            lborderCancelRegist.Visibility = System.Windows.Visibility.Hidden;
        }

        private void btnExecuteCancelRegist_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(txtbCancelRegistDate.Text + "に登録したデータを削除しますか？", "キャンセル実行確認", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
                return;

            DateTime dt = Convert.ToDateTime(txtbCancelRegistDate.Text);
            try
            {
                Account account = new Account();
                MoneyInputRegist reg = new MoneyInputRegist(null, account, new DbConnection());
                reg.Cancel(dt);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "");
            }
        }

        private void btnDetailAdd_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuitemArrangementArrear_Click(object sender, RoutedEventArgs e)
        {
            ColViewListInputDataDetail.Filter = delegate(object o)
            {
                MakeupDetailData data = o as MakeupDetailData;

                //Debug.Print("dispinfoFilterCreditCode [" + dispinfoFilterCreditCode + "]    data.CreditCode [" + data.CreditCode + "]");
                if (data.Kind == 4)
                    return true;

                return false;
            };

            ColViewListInputDataDetail.SortDescriptions.Clear();
            ColViewListInputDataDetail.SortDescriptions.Add(new SortDescription("DataOrder", ListSortDirection.Ascending));

            DateTime beforeDate = new DateTime(1900,1,1);
            MakeupDetailData defaultMakeupData = null;
            string message = "";

            // 更新後に金額チェックをするため、最後のBalance金額を取得
            long balanceLast = 0;
            foreach (MakeupDetailData data in ColViewListInputDataDetail)
                balanceLast = data.Balance;

            foreach (MakeupDetailData data in ColViewListInputDataDetail)
            {
                // 初期データ[ID==0]は別に格納する
                if (data.DataOrder == 0)
                {
                    defaultMakeupData = data;
                    if (message.Length > 0)
                        break;
                }

                if (data.Date < beforeDate && data.DataOrder != 0)
                {
                    message = "日付が不正なデータが存在します、 No[" + data.DataOrder + "] より先のデータを日付毎に修正しますか？\n" + data.Date.ToString("yyyy/MM/dd") + " > " + beforeDate.ToString("yyyy/MM/dd");
                    if (defaultMakeupData != null)
                        break;
                }

                beforeDate = data.Date;
            }

            if (message.Length <= 0)
            {
                MessageBox.Show("全データが日付順で整理済み");
                return;
            }

            MessageBoxResult result = MessageBox.Show(message, "整理開始確認", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;

            ColViewListInputDataDetail.SortDescriptions.Clear();
            ColViewListInputDataDetail.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            ColViewListInputDataDetail.SortDescriptions.Add(new SortDescription("DataOrder", ListSortDirection.Ascending));

            long balance = 0, balanceAdd = 0;

            // データベース：トランザクションを開始
            dbcon.BeginTransaction("ARREAR_ARRANGEMENT");
            try
            {
                balance = defaultMakeupData.Balance;
                int DataOrder = 1;
                foreach (MakeupDetailData data in ColViewListInputDataDetail)
                {
                    if (data.DataOrder == 0)
                        continue;

                    if (data.DataOrder == -1)
                    {
                        balanceAdd += data.Amount;
                    }

                    if (data.DebitCode.IndexOf("1201") == 0)
                        balance = balance - data.Amount;
                    else
                        balance = balance + data.Amount;

                    if (data.DataOrder != DataOrder || data.Balance != balance)
                    {
                        Debug.Print("No[" + data.DataOrder + "] balance [" + balance + "]");
                        data.DataOrder = DataOrder;
                        data.Balance = balance;
                        Arrear.Update(data, dbcon);
                    }
                    //else
                    //    Debug.Print("No[" + data.DataOrder + "] balance [" + balance + "]");

                    DataOrder++;
                }

                if (balance != balanceLast)
                {
                    MessageBoxResult mbResult = MessageBox.Show("最後の金額が合わないが、処理を継続しますか？\n  計算結果 [" + balance + "]  現在の最後の残高 [" + balanceLast + "] 追加データ合計 [" + balanceAdd + "]", "金額不一致確認", MessageBoxButton.OKCancel);

                    if (mbResult == MessageBoxResult.Cancel)
                    {
                        dbcon.RollbackTransaction();
                        return;
                    }
                }
            }
            catch (SqlException sqlex)
            {
                Debug.Write(sqlex);
                dbcon.RollbackTransaction();
                MessageBox.Show("SqlException発生、ロールバックします\n" + sqlex.Message);
                return;
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
                dbcon.RollbackTransaction();
                MessageBox.Show("Exception発生、ロールバックします\n" + ex.Message);
                return;
            }
            dbcon.CommitTransaction();
        }

        private void dgridSelectDate_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConditionFromDate = Convert.ToDateTime(dispctrlMakeupScopeDate[0]);
            ConditionToDate = Convert.ToDateTime(dispctrlMakeupScopeDate[1]);

            lborderSetMakeupScopeDate.Visibility = System.Windows.Visibility.Hidden;

            DateTime selDt = (DateTime)dgridSelectDate.SelectedItem;
            DateTime beforeDt = new DateTime(1900, 1, 1);
            foreach (DateTime dt in listMakeupScopeDate)
            {
                if (selDt.CompareTo(dt) == 0)
                    break;

                beforeDt = dt.AddDays(-1);
            }

            // 一番先頭の場合はTO日付には1ヶ月後を設定
            if (beforeDt.Year == 1900)
                beforeDt = selDt.AddMonths(1);

            txtbMakeScopeDate.Text = selDt.ToString("yyyy/MM/dd") + "～" + beforeDt.ToString("yyyy/MM/dd");
            txtbTermDate.Text = selDt.ToString("yyyy/MM/dd") + "～" + beforeDt.ToString("yyyy/MM/dd");

            MakeSummary(selDt, beforeDt);
        }

        private void MakeSummary(DateTime myFrom, DateTime myTo)
        {
            lgridSummary.Children.Clear();
            lgridSummary.RowDefinitions.Clear();

            RowDefinition rowHeader = new RowDefinition();

            Summary summary = new Summary(myFrom, myTo, listInputDataDetail);

            lgridSummary.RowDefinitions.Add(new RowDefinition());
            int cnt = 1;
            listSummaryParameter = summary.listSummaryParameter;
            foreach (SummaryParameter summaryParameter in listSummaryParameter)
            {
                if (summaryParameter.Total > 0
                    || summaryParameter.Kind == 1
                    || summaryParameter.Kind == 2)
                {
                    RowDefinition row = new RowDefinition();
                    row.Height = new GridLength(SummaryUi.GetGridLength(summaryParameter.Kind));
                    lgridSummary.RowDefinitions.Add(row);

                    lgridSummary.Children.Add(SummaryUi.GetCaptionTextBlock(summaryParameter.Kind, summaryParameter.Name, cnt));

                    if (summaryParameter.Total > 0)
                        lgridSummary.Children.Add(SummaryUi.GetAmountTextBlock(summaryParameter.Kind, summaryParameter.Total, cnt, false));

                    if (summaryParameter.SubTotal > 0)
                        lgridSummary.Children.Add(SummaryUi.GetAmountTextBlock(summaryParameter.Kind, summaryParameter.SubTotal, cnt, true));

                    cnt++;
                }
            }
        }

        private void lgridSummary_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock textblock = e.OriginalSource as TextBlock;

            if (textblock == null)
                return;

            object obj = textblock.GetValue(Grid.ColumnProperty);
            _logger.Debug(obj.ToString() + "  textblock.Text " + textblock.Text);

            List<int> idList = null;
            foreach (SummaryParameter param in listSummaryParameter)
            {
                if (param.Name.Equals(textblock.Text))
                {
                    idList = param.MatchId;
                    break;
                }
            }
            if (idList != null)
            {
                string idListLog = "";
                foreach (int id in idList)
                    idListLog += id + ",";

                _logger.Debug(idListLog);

                ColViewListInputDataDetail.Filter = delegate (object o)
                {
                    MakeupDetailData data = o as MakeupDetailData;

                    if (data.Kind != 1)
                        return false;
                    if (idList.Contains(data.Id))
                        return true;

                    return false;
                };
                //ソートするとlistDataの件数が増える
                ColViewListInputDataDetail.SortDescriptions.Clear();
                ColViewListInputDataDetail.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));

                List<MakeupDetailData> listData = ColViewListInputDataDetail.Cast<MakeupDetailData>().ToList();
                _logger.Debug("Match件数 [" + listData.Count + "]");

                dispctrlMakeupDetailMode = MAKEUPDETAIL_MODE_MAKEUPDETAIL;
                SwitchLayout(LAYOUTMODE_MAKEUP_TARGET_DETAIL);

                DataGridMakeupDetailWidthSetting();

                lgridSummaryEveryAccount.RowDefinitions.Clear();
                lgridSummaryEveryAccount.Children.Clear();
                SummaryEveryAccount summaryEveryAccount = new SummaryEveryAccount(listData, account);

                int cnt = 0;
                int maxRow = (summaryEveryAccount.DebitCount > summaryEveryAccount.CreditCount) ? summaryEveryAccount.DebitCount : summaryEveryAccount.CreditCount;

                for(cnt=0; cnt<maxRow; cnt++)
                {
                    RowDefinition row = new RowDefinition();
                    row.Height = new GridLength(40);
                    lgridSummaryEveryAccount.RowDefinitions.Add(row);
                }

                cnt = 0;
                foreach (SummaryEveryAccountData data in summaryEveryAccount.ColViewData)
                {
                    if (data.Kind == 1)
                    {
                        lgridSummaryEveryAccount.Children.Add(SummaryEverryAccountUi.GetCaptionTextBlock(data.Code, 1, cnt));

                        lgridSummaryEveryAccount.Children.Add(SummaryEverryAccountUi.GetCaptionTextBlock(data.Name, 2, cnt));

                        lgridSummaryEveryAccount.Children.Add(SummaryEverryAccountUi.GetAmountTextBlock(data.Total, 3, cnt));

                        cnt++;
                    }
                }
                cnt = 0;
                foreach (SummaryEveryAccountData data in summaryEveryAccount.ColViewData)
                {
                    if (data.Kind == 2)
                    {
                        lgridSummaryEveryAccount.Children.Add(SummaryEverryAccountUi.GetCaptionTextBlock(data.Code, 5, cnt));

                        lgridSummaryEveryAccount.Children.Add(SummaryEverryAccountUi.GetCaptionTextBlock(data.Name, 6, cnt));

                        lgridSummaryEveryAccount.Children.Add(SummaryEverryAccountUi.GetAmountTextBlock(data.Total, 7, cnt));

                        cnt++;
                    }
                }
            }
        }
    }
}
