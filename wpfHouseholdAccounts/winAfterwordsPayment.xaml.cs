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
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Diagnostics;

namespace wpfHouseholdAccounts
{
    /// <summary>
    /// winAfterwordsPayment.xaml の相互作用ロジック
    /// </summary>
    public partial class winAfterwordsPayment : Window
    {
        Environment env;

        Account account;
        List<AccountData> listAccount;

        List<AfterwordsPaymentData> listAfterwordsPayment;
        AfterwordsPaymentData dispinfoSelectAfterwordsPaymentData;
        List<AfterwordsPaymentData> dispinfoSelectListAfterwordsPaymentData;
        ICollectionView ColViewListAfterwordsPayment;

        List<PaymentData> listDecision;
        PaymentData dispinfoSelectDecisionData;
        List<PaymentData> dispinfoSelectListDecisionData;
        ICollectionView ColViewListDecision;

        // 画面情報（UIElementと同期）
        class DisplayInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }

            public DisplayInfo()
            {
                CardDecisionAmount = 0;
            }

            private long _CardDecisionAmount;
            public long CardDecisionAmount
            {
                get
                {
                    return _CardDecisionAmount;
                }
                set
                {
                    _CardDecisionAmount = value;
                    NotifyPropertyChanged("CardDecisionAmount");
                }
            }
        }

        DisplayInfo dispinfoData;
        double dispinfolgridMainHeight;
        string dispinfoFilterCreditCode;
        string dispinfoDecisionButton = "";
        bool dispinfoIsTotalDisplayCardDifference = true;
        bool dispctrlDataGridRefreshAfterwordsPayment = false;
        bool dispctrlDataGridRefreshDecision = false;
        int dispinfoFilterKind;

        bool IsEdit;

        public winAfterwordsPayment()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dispinfoData = new DisplayInfo();
            DataContext = dispinfoData;

            env = new Environment();

            listAfterwordsPayment = AfterwordsPayment.GetData();
            dgridAfterwordsPayment.ItemsSource = listAfterwordsPayment;

            account = new Account();

            // リストボックスへ費用項目の表示
            Binding items = new Binding();

            items.Source = account.GetTwoDigitItems();
            items.Path = new PropertyPath(".");

            lstAccountExpense.SetBinding(ItemsControl.ItemsSourceProperty, items);

            listAccount = account.GetItems();
            lstAccountDetail.ItemsSource = listAccount;

            IsEdit = false;

            dgridAfterwordsPayment.Columns[9].Width = new DataGridLength(CalcurateColumnWidth(dgridAfterwordsPayment));
            dgridDecision.Columns[6].Width = new DataGridLength(CalcurateColumnWidth(dgridDecision));

            dispinfoFilterCreditCode = "";
            dispinfoFilterKind = -1;

            dispctrlDataGridRefreshDecision = true;

            SwitchLayout(0);
        }

        private const int LAYOUTMODE_DATAGRID_AFTERWORDS = 1;
        private const int LAYOUTMODE_DATAGRID_AFTERWORDS_WITHCTRL = 2;
        private const int LAYOUTMODE_DATAGRID_DECISION = 3;
        private const int LAYOUTMODE_NEWREGIST = 11;
        private const int LAYOUTMODE_EACHDATE = 12;

        /// <summary>
        /// SwitchLayoutの画面呼び出し用のWrapperメソッド
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            SwitchLayout(0);
        }

        private void SetViewFilterAndSort()
        {
            if (dgridAfterwordsPayment.Visibility == System.Windows.Visibility.Visible)
            {
                ColViewListAfterwordsPayment = CollectionViewSource.GetDefaultView(listAfterwordsPayment);

                ColViewListAfterwordsPayment.Filter = delegate(object o)
                {
                    AfterwordsPaymentData data = o as AfterwordsPaymentData;

                    if (dispinfoFilterCreditCode.Length > 0
                        && data.CreditCode.IndexOf(dispinfoFilterCreditCode) != 0)
                        return false;

                    if (dispinfoDecisionButton.Equals("確定候補")
                        && data.DisplayDecisionDate.Length <= 0)
                        return false;

                    if (dispinfoFilterKind > 0
                        && data.Kind != dispinfoFilterKind)
                        return false;

                    return true;
                };

                if (dispinfoDecisionButton.Equals("確定候補"))
                {
                    ColViewListAfterwordsPayment.SortDescriptions.Clear();
                    ColViewListAfterwordsPayment.SortDescriptions.Add(new SortDescription("Area", ListSortDirection.Ascending));
                    ColViewListAfterwordsPayment.SortDescriptions.Add(new SortDescription("DecisionDate", ListSortDirection.Ascending));
                    ColViewListAfterwordsPayment.SortDescriptions.Add(new SortDescription("OrderSameDate", ListSortDirection.Ascending));
                }
                else
                {
                    ColViewListAfterwordsPayment.SortDescriptions.Clear();
                    ColViewListAfterwordsPayment.SortDescriptions.Add(new SortDescription("Area", ListSortDirection.Ascending));
                    ColViewListAfterwordsPayment.SortDescriptions.Add(new SortDescription("LastTimePaymentDate", ListSortDirection.Ascending));
                    ColViewListAfterwordsPayment.SortDescriptions.Add(new SortDescription("OrderSameDate", ListSortDirection.Ascending));
                }
            }
            else
            {
                ColViewListDecision = CollectionViewSource.GetDefaultView(listDecision);

                ColViewListDecision.Filter = delegate(object o)
                {
                    PaymentData data = o as PaymentData;

                    //Debug.Print("dispinfoFilterCreditCode [" + dispinfoFilterCreditCode + "]    data.CreditCode [" + data.CreditCode + "]");
                    if (dispinfoFilterCreditCode.Length > 0
                        && data.CreditCode.IndexOf(dispinfoFilterCreditCode) != 0)
                        return false;

                    return true;
                };

                ColViewListDecision.SortDescriptions.Clear();
                ColViewListDecision.SortDescriptions.Add(new SortDescription("InputDate", ListSortDirection.Ascending));
                ColViewListDecision.SortDescriptions.Add(new SortDescription("PaymentDate", ListSortDirection.Ascending));
            }
        }

        /// <summary>
        /// 確定データ時以外の確定データ登録ボタン領域の各種情報の切り替え
        /// </summary>
        private void SwitchLayoutPaymentDecisionControl(int myMode)
        {
            if (myMode == LAYOUTMODE_DATAGRID_AFTERWORDS_WITHCTRL)
            {
                if (dispinfoFilterCreditCode.Equals("102"))
                {
                    txtCardDecisionAmount.Visibility = System.Windows.Visibility.Hidden;
                    txtbCardDifference.Visibility = System.Windows.Visibility.Hidden;
                    dtpickerDecisionSchedule.Visibility = System.Windows.Visibility.Hidden;
                    txtbTotalInfo.Margin = new Thickness(5, 0, 10, -40);
                }
                else
                {
                    txtCardDecisionAmount.Visibility = System.Windows.Visibility.Visible;
                    txtbCardDifference.Visibility = System.Windows.Visibility.Visible;
                    dtpickerDecisionSchedule.Visibility = System.Windows.Visibility.Visible;
                    txtbTotalInfo.Margin = new Thickness(5, 0, 10, 0);
                }
            }
            else
            {
                txtbTotalInfo.Margin = new Thickness(5, 0, 10, 0);
            }
        }

        /// <summary>
        /// 確定データ時の確定実行ボタン領域の各種情報の切り替え
        /// </summary>
        private void SwitchLayoutDecisionControl()
        {
            bool IsDecided = false;
            string displaydate = "";
            DateTime date = new DateTime(1900,1,1);
            string displaycode = "";

            foreach (PaymentData data in ColViewListDecision.Cast<PaymentData>())
            {
                if (data.PaymentDate.Year >= 2000)
                {
                    IsDecided = true;
                    displaydate = data.PaymentDate.ToString("yyyy/MM/dd");
                    date = data.PaymentDate;
                    displaycode = data.CreditCode;
                    break;
                }
            }

            //Debug.Print("IsDecided " + IsDecided);

            if (IsDecided)
            {
                // 親画面の表示データ更新
                PaymentData matchdata = ((MainWindow)this.Owner).OwnerGetPaymentSchedule(displaycode, date);

                string amount = "";
                if (matchdata != null)
                    amount = String.Format("{0:###,###,##0}", matchdata.Amount);

                btnDecisionExecute.IsEnabled = false;
                dtpickerDecision.Visibility = System.Windows.Visibility.Hidden;
                txtbDecisionDate.Text = "確定日 " + displaydate;
                txtbDecisionDate.Visibility = System.Windows.Visibility.Visible;
                txtbDecisionAmount.Background = Brushes.Honeydew;
                txtbDecisionAmount.Text = amount;
                txtbEnvAmount.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                btnDecisionExecute.IsEnabled = true;
                dtpickerDecision.Visibility = System.Windows.Visibility.Visible;
                txtbDecisionDate.Visibility = System.Windows.Visibility.Hidden;
                txtbDecisionAmount.Background = Brushes.Azure;
                txtbEnvAmount.Visibility = System.Windows.Visibility.Visible;
            }
        }

        // 新規入力LayoutGridの表示
        // 複数日付LayoutGrid、dgridAfterwordsPaymentのハーフモード（dgridAfterwordsPayment 上半分表示、lgridEditEachDate 表示）
        // 確定候補dgridAfterwordsPaymentの表示
        // 確定データdgridDecisionの表示
        // 0の場合はToggleButtonの状態からDataGrid表示を判断する
        private void SwitchLayout(int myMode)
        {
            // IsEditについて
            //   IsEdit = trueはmenuitemEditRow_Clickのみなので
            //   myModeで0が設定されている場合はfalseにして問題なし（他のfalseで不要かも知れないが一応false設定）
            //   それ以外はLAYOUTMODE_NEWREGIST以外は全て明示的にfalseを設定する
            //  LAYOUTMODE_NEWREGISTはtrueが設定されている場合があるので、未設定とする

            int mode = 0;
            if (myMode == 0)
            {
                mode = GetDataGridState();
                IsEdit = false;
            }
            else
                mode = myMode;

            if (mode == LAYOUTMODE_NEWREGIST)
            {
                lgridEdit.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                lgridEdit.Visibility = System.Windows.Visibility.Hidden;
            }

            if (mode == LAYOUTMODE_EACHDATE)
            {
                lgridEditEachDate.Visibility = System.Windows.Visibility.Visible;

                dispinfolgridMainHeight = lgridMain.RowDefinitions[3].ActualHeight;
                lgridEditEachDate.Visibility = System.Windows.Visibility.Visible;
                lgridEdit.Visibility = System.Windows.Visibility.Hidden;
                dgridAfterwordsPayment.Height = 100;

                IsEdit = false;

                mode = LAYOUTMODE_DATAGRID_AFTERWORDS;
            }
            else
            {
                lgridEditEachDate.Visibility = System.Windows.Visibility.Hidden;
                dgridAfterwordsPayment.Height = Double.NaN;
            }

            if (mode == LAYOUTMODE_DATAGRID_AFTERWORDS
                || mode == LAYOUTMODE_DATAGRID_AFTERWORDS_WITHCTRL)
            {
                dgridAfterwordsPayment.Visibility = System.Windows.Visibility.Visible;
                dgridDecision.Visibility = System.Windows.Visibility.Hidden;

                if (dispctrlDataGridRefreshAfterwordsPayment)
                {
                    listAfterwordsPayment = AfterwordsPayment.GetData();
                    dgridAfterwordsPayment.ItemsSource = listAfterwordsPayment;
                    dgridAfterwordsPayment.Items.Refresh();

                    dispctrlDataGridRefreshAfterwordsPayment = false;
                }

                if (mode == LAYOUTMODE_DATAGRID_AFTERWORDS_WITHCTRL)
                {
                    if (dispinfoFilterCreditCode.Length > 0)
                    {
                        // Envから取得して確定用の日付、金額を設定
                        object[] objData = env.GetCardDecision(dispinfoFilterCreditCode);

                        if (objData[0] != null)
                            dtpickerDecisionSchedule.SelectedDate = (DateTime)objData[0];
                        dispinfoData.CardDecisionAmount = Convert.ToInt64(objData[1]);
                    }

                    lgridMain.RowDefinitions[3].Height = new GridLength(40);
                    lgridPaymentDecision.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    lgridMain.RowDefinitions[3].Height = new GridLength(0);
                    lgridPaymentDecision.Visibility = System.Windows.Visibility.Hidden;
                }

                // 最後に実行
                SetViewFilterAndSort();

                // 確定データ登録ボタン領域の各種表示
                SwitchLayoutPaymentDecisionControl(mode);

                IsEdit = false;
            }
            else
            {
                dgridAfterwordsPayment.Visibility = System.Windows.Visibility.Hidden;
                lgridPaymentDecision.Visibility = System.Windows.Visibility.Hidden;
            }

            if (mode == LAYOUTMODE_DATAGRID_DECISION)
            {
                if (dispctrlDataGridRefreshDecision == true)
                {
                    listDecision = Loan.GetListDetail();
                    dgridDecision.ItemsSource = listDecision;

                    dispctrlDataGridRefreshDecision = false;
                }

                if (dispinfoFilterCreditCode.Length > 0)
                {
                    // Envから取得して確定用の日付、金額を設定
                    object[] objData = env.GetCardDecision(dispinfoFilterCreditCode);

                    if (objData[0] != null)
                        dtpickerDecision.SelectedDate = (DateTime)objData[0];
                    txtbEnvAmount.Text = String.Format("{0:###,###,##0}", Convert.ToInt64(objData[1]));
                }

                dgridDecision.Visibility = System.Windows.Visibility.Visible;
                lgridMain.RowDefinitions[3].Height = new GridLength(40);
                lgridDecisionExecute.Visibility = System.Windows.Visibility.Visible;

                SetViewFilterAndSort();

                // 確定実行ボタン領域の各種情報の表示処理
                SwitchLayoutDecisionControl();
            }
            else
            {
                dgridDecision.Visibility = System.Windows.Visibility.Hidden;
                lgridDecisionExecute.Visibility = System.Windows.Visibility.Hidden;
            }
            // 画面入力用のLayout
            //   複数日付 lgridEditEachDate
            //   新規登録・編集 lgridEdit

            // DataGrid 確定データ dgridDecision, ボタン類 lgridDecisionExecute
            // DataGrid 後日支払予定 dgridAfterwordsPayment, ボタン類 lgridPaymentDecision
        }

        private int GetDataGridState()
        {
            if (dispinfoDecisionButton.Equals("確定候補")
                || dispinfoFilterCreditCode.Equals("102"))
                return LAYOUTMODE_DATAGRID_AFTERWORDS_WITHCTRL;
            if (dispinfoDecisionButton.Equals("確定データ"))
                return LAYOUTMODE_DATAGRID_DECISION;

            return LAYOUTMODE_DATAGRID_AFTERWORDS;
        }

        private double CalcurateColumnWidth(DataGrid datagrid)
        {
            double winX = this.ActualWidth;
            double colTotal = 0;
            foreach (DataGridColumn col in datagrid.Columns)
            {
                if (col.Header.Equals("摘要"))
                    continue;

                DataGridLength colw = col.Width;
                double w = colw.DesiredValue;
                colTotal += w;
            }

            return winX - colTotal;
        }

        private void OnGotKeyboardFocusCode(object sender, KeyboardFocusChangedEventArgs e)
        {
            lstAccountExpense.Visibility = System.Windows.Visibility.Visible;
            lstAccountDetail.Visibility = System.Windows.Visibility.Visible;

            TextBox txtbox = sender as TextBox;

            ICollectionView view = CollectionViewSource.GetDefaultView(listAccount);
            if (txtbox.Name.ToString().Equals("txtDebitCode"))
                new TextSearchFilterForGrid(view, this.txtDebitCode);
            else
                new TextSearchFilterForGrid(view, this.txtCreditCode);
        }

        private void OnLostKeyboardFocusCode(object sender, KeyboardFocusChangedEventArgs e)
        {
            lstAccountExpense.Visibility = System.Windows.Visibility.Collapsed;
            lstAccountDetail.Visibility = System.Windows.Visibility.Collapsed;
            TextBox txtbox = sender as TextBox;
            AccountData data = account.GetItemFromCode(txtbox.Text);

            string name = txtbox.Name.ToString();
            if (data != null)
            {
                if (name.Equals("txtDebitCode"))
                    txtDebitName.Text = data.Name;
                else
                    txtCreditName.Text = data.Name;
            }
            else
            {
                if (name.Equals("txtDebitCode"))
                    txtDebitName.Text = "";
                else
                    txtCreditName.Text = "";
            }
        }

        private void btnRegist_Click(object sender, RoutedEventArgs e)
        {
            AccountData accountdata = account.GetItemFromCode(txtDebitCode.Text);
            if (accountdata == null || accountdata.Name == null)
            {
                MessageBox.Show("借方コードの入力されているコードはマスタに存在しないコードです");
                return;
            }
            accountdata = account.GetItemFromCode(txtCreditCode.Text);
            if (accountdata == null || accountdata.Name == null)
            {
                MessageBox.Show("貸方コードの入力されているコードはマスタに存在しないコードです");
                return;
            }

            long amount = 0;
            try
            {
                amount = Convert.ToInt64(txtAmount.Text);
            }
            catch(Exception)
            {
                MessageBox.Show("金額が不正です");
                return;
            }

            int kind = 0;
            try
            {
                kind = Convert.ToInt32(cmbKind.SelectedValue);
            }
            catch (Exception)
            {
            }
            if (!(kind > 0 && kind < 6))
            {
                MessageBox.Show("種別が未選択です");
                return;
            }


            int order = 99;
            try
            {
                if (txtOrderSameDate.Text.Trim().Length > 0)
                    order = Convert.ToInt32(txtOrderSameDate.Text);
            }
            catch(Exception)
            {
                MessageBox.Show("順番が不正です");
                return;
            }

            int area = 0;
            try
            {
                area = Convert.ToInt32(cmbArea.SelectedValue);
            }
            catch (Exception)
            {
            }
            DateTime dtDecision = new DateTime(1900,1,1);
            if (txtDecisionDate.Text.Length > 0)
            {
                try
                {
                    dtDecision = Convert.ToDateTime(txtDecisionDate.Text);
                }
                catch (Exception)
                {
                    MessageBox.Show("確定日が不正です");
                    return;
                }
            }
            if (IsEdit)
            {
                dispinfoSelectAfterwordsPaymentData.DebitCode = txtDebitCode.Text;
                dispinfoSelectAfterwordsPaymentData.CreditCode = txtCreditCode.Text;
                dispinfoSelectAfterwordsPaymentData.Amount = amount;
                dispinfoSelectAfterwordsPaymentData.Remark = txtRemark.Text;
                dispinfoSelectAfterwordsPaymentData.Kind = kind;
                dispinfoSelectAfterwordsPaymentData.Area = area;
                dispinfoSelectAfterwordsPaymentData.OrderSameDate = order;
                dispinfoSelectAfterwordsPaymentData.DecisionDate = dtDecision;

                dispinfoSelectAfterwordsPaymentData.DbUpdate(new DbConnection());
            }
            else
            {
                AfterwordsPaymentData workdata = new AfterwordsPaymentData();

                workdata.DebitCode = txtDebitCode.Text;
                workdata.CreditCode = txtCreditCode.Text;
                workdata.Amount = amount;
                workdata.Remark = txtRemark.Text;
                workdata.Kind = kind;
                workdata.Area = area;
                workdata.OrderSameDate = order;
                workdata.DecisionDate = dtDecision;

                workdata.DbInsert(new DbConnection());
            }

            // StackPanel内のToggleButtonを取得
            List<TextBox> listTextBox = CommonMethod.FindVisualChild<TextBox>(lgridEdit, "TextBox");

            // ToggleButtonの押下状態を変更する
            //   全ての場合は全て以外はOFF状態
            //   全て意外はsenderと同じ（押下されたTButton）ではないのは全てOFF状態
            foreach (TextBox tbox in listTextBox)
                tbox.Text = "";

            cmbKind.SelectedValue = 0;
            cmbArea.SelectedValue = 0;

            // 表示前の条件設定
            dispctrlDataGridRefreshAfterwordsPayment = true; // データ更新をかける

            // lgridEditをCloseしてDataGrid表示へ戻す
            SwitchLayout(0);
        }

        private void btnAddNew_Click(object sender, RoutedEventArgs e)
        {
            // lgridEditを表示
            SwitchLayout(LAYOUTMODE_NEWREGIST);
        }

        private void menuitemEditRow_Click(object sender, RoutedEventArgs e)
        {
            txtDebitCode.Text = dispinfoSelectAfterwordsPaymentData.DebitCode;
            AccountData accountdata = account.GetItemFromCode(dispinfoSelectAfterwordsPaymentData.DebitCode);
            if (accountdata != null && accountdata.Name != null)
                txtDebitName.Text = accountdata.Name;
            txtCreditCode.Text = dispinfoSelectAfterwordsPaymentData.CreditCode;
            accountdata = account.GetItemFromCode(dispinfoSelectAfterwordsPaymentData.CreditCode);
            if (accountdata != null && accountdata.Name != null)
                txtCreditName.Text = accountdata.Name;
            txtAmount.Text = Convert.ToString(dispinfoSelectAfterwordsPaymentData.Amount);
            txtRemark.Text = dispinfoSelectAfterwordsPaymentData.Remark;
            cmbKind.SelectedValue = dispinfoSelectAfterwordsPaymentData.Kind;
            txtOrderSameDate.Text = Convert.ToString(dispinfoSelectAfterwordsPaymentData.OrderSameDate);
            cmbArea.SelectedValue = dispinfoSelectAfterwordsPaymentData.Area;
            txtDecisionDate.Text = dispinfoSelectAfterwordsPaymentData.DisplayDecisionDate;

            IsEdit = true;

            // lgridEditを表示
            SwitchLayout(LAYOUTMODE_NEWREGIST);
        }

        private void OnTBtnClick_TermFilter(object sender, RoutedEventArgs e)
        {
            ToggleButton senderButton = sender as ToggleButton;

            string ButtonLabel = senderButton.Content.ToString();

            List<ToggleButton> list = CommonMethod.FindVisualChild<ToggleButton>(lstackTerm, "ToggleButton");

            // ToggleButtonの押下状態を変更する
            //   全ての場合は全て以外はOFF状態
            //   全て意外はsenderと同じ（押下されたTButton）ではないのは全てOFF状態
            foreach (ToggleButton tbtn in list)
            {
                if (ButtonLabel.Equals("全て"))
                {
                    if (!tbtn.Content.Equals("全て"))
                        tbtn.IsChecked = false;
                }
                else
                {
                    if (ButtonLabel.Equals(tbtn.Content))
                        tbtn.IsChecked = true;
                    else
                        tbtn.IsChecked = false;
                }
            }

            if (senderButton.Content.Equals("確定候補"))
            {
                if (senderButton.IsChecked != null)
                {
                    bool isChecked = (bool)senderButton.IsChecked;

                    if (isChecked)
                        dispinfoDecisionButton = "確定候補";
                    else
                        dispinfoDecisionButton = "";

                    List<ToggleButton> listtbtndeci = CommonMethod.FindVisualChild<ToggleButton>(lstackDecision, "ToggleButton");
                    foreach (ToggleButton tbtn in listtbtndeci)
                    {
                        if (tbtn.Content.Equals("確定データ"))
                            tbtn.IsChecked = false;
                    }
                }
            }
            else if (senderButton.Content.Equals("確定データ"))
            {
                if (senderButton.IsChecked != null)
                {
                    bool isChecked = (bool)senderButton.IsChecked;

                    if (isChecked)
                        dispinfoDecisionButton = "確定データ";
                    else
                        dispinfoDecisionButton = "";

                    List<ToggleButton> listtbtndeci = CommonMethod.FindVisualChild<ToggleButton>(lstackDecision, "ToggleButton");
                    foreach (ToggleButton tbtn in listtbtndeci)
                    {
                        if (tbtn.Content.Equals("確定候補"))
                            tbtn.IsChecked = false;
                    }
                }
            }

            int[] arrKind = { 1, 2, 3, 4, 5 };
            string[] arrLabel = { "月必須", "年必須", "不定期", "複数日付設定", "一時" };

            dispinfoFilterKind = 0;
            for (int idx = 0; idx < arrKind.Length; idx++ )
            {
                if (arrLabel[idx].Equals(ButtonLabel))
                    dispinfoFilterKind = arrKind[idx];
            }

            SwitchLayout(0);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            dgridAfterwordsPayment.Columns[9].Width = new DataGridLength(CalcurateColumnWidth(dgridAfterwordsPayment));
            dgridDecision.Columns[6].Width = new DataGridLength(CalcurateColumnWidth(dgridDecision));
        }

        private void dgridAfterwordsPayment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selcount = 0;
            if (dgridAfterwordsPayment.SelectedItems.Count > 1)
            {
                dispinfoSelectListAfterwordsPaymentData = new List<AfterwordsPaymentData>();
                foreach (AfterwordsPaymentData data in dgridAfterwordsPayment.SelectedItems)
                    dispinfoSelectListAfterwordsPaymentData.Add(data);

                dispinfoSelectAfterwordsPaymentData = null;
                selcount = dispinfoSelectListAfterwordsPaymentData.Count();
            }
            else
            {
                dispinfoSelectAfterwordsPaymentData = (AfterwordsPaymentData)dgridAfterwordsPayment.SelectedItem;
                dispinfoSelectListAfterwordsPaymentData = null;
                selcount = 1;
            }

            int idx = 0;
            long total = 0;

            if (ColViewListAfterwordsPayment != null)
            {
                foreach (AfterwordsPaymentData data in ColViewListAfterwordsPayment.Cast<AfterwordsPaymentData>())
                {
                    total += data.Amount;
                    idx++;
                }
            }
            else
            {
                foreach (AfterwordsPaymentData data in dgridAfterwordsPayment.ItemsSource)
                {
                    total += data.Amount;
                    idx++;
                }
            }
            if (dgridAfterwordsPayment.SelectedItems == null)
            {
                txtbTotalInfo.Text = "合計 " + idx + "件  " + String.Format("{0:###,###,##0}", total);
                return;
            }
            long seltotal = 0;
            foreach (AfterwordsPaymentData data in dgridAfterwordsPayment.SelectedItems)
                seltotal += data.Amount;

            txtbTotalInfo.Text = "選択 " + selcount + "件  " + String.Format("{0:###,###,##0}", seltotal) + " ／ 合計 " + idx + "件  " + String.Format("{0:###,###,##0}", total);

            if (dispinfoIsTotalDisplayCardDifference)
            {
                long diff = dispinfoData.CardDecisionAmount - total;
                txtbCardDifference.Text = String.Format("{0:###,###,##0}", diff);
            }
            else
            {
                long diff = dispinfoData.CardDecisionAmount - seltotal;
                txtbCardDifference.Text = String.Format("{0:###,###,##0}", diff);
            }
        }

        private void dgridDecision_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 確定済みの場合は、txtbDecisionAmountには支払金額が表示されているので本メソッドでの金額更新はしない
            if (!btnDecisionExecute.IsEnabled)
                return;

            int selcount = 0;
            if (dgridDecision.SelectedItems.Count > 1)
            {
                dispinfoSelectListDecisionData = new List<PaymentData>();
                foreach (PaymentData data in dgridDecision.SelectedItems)
                    dispinfoSelectListDecisionData.Add(data);

                dispinfoSelectDecisionData = null;
                selcount = dispinfoSelectListDecisionData.Count();
            }
            else
            {
                dispinfoSelectDecisionData = (PaymentData)dgridDecision.SelectedItem;
                dispinfoSelectListAfterwordsPaymentData = null;

                dispinfoSelectListDecisionData = new List<PaymentData>();
                dispinfoSelectListDecisionData.Add(dispinfoSelectDecisionData);

                selcount = 1;
            }

            int idx = 0;
            long total = 0;

            if (ColViewListDecision != null)
            {
                foreach (PaymentData data in ColViewListDecision.Cast<PaymentData>())
                {
                    total += data.Amount;
                    idx++;
                }
            }
            else
            {
                foreach (PaymentData data in dgridDecision.ItemsSource)
                {
                    total += data.Amount;
                    idx++;
                }
            }
            if (dgridDecision.SelectedItems == null)
            {
                txtbTotalInfo.Text = "合計 " + idx + "件  " + String.Format("{0:###,###,##0}", total);
                return;
            }

            long seltotal = 0;
            foreach (PaymentData data in dgridDecision.SelectedItems)
                seltotal += data.Amount;

            txtbTotalInfo.Text = "選択 " + selcount + "件  " + String.Format("{0:###,###,##0}", seltotal) + " ／ 合計 " + idx + "件  " + String.Format("{0:###,###,##0}", total);
            txtbDecisionAmount.Text = String.Format("{0:###,###,##0}", seltotal);
        }

        private void menuitemDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("行を削除しますか？", "削除確認", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
                return;

            if (dispinfoSelectListAfterwordsPaymentData != null)
            {
                DbConnection dbcon = new DbConnection();
                dbcon.BeginTransaction("ROWS_DELETE");
                foreach (AfterwordsPaymentData data in dispinfoSelectListAfterwordsPaymentData)
                {
                    data.DbDelete(dbcon);
                    //listAfterwordsPayment.Remove(data);
                }

                dbcon.CommitTransaction();

                foreach (AfterwordsPaymentData data in dispinfoSelectListAfterwordsPaymentData)
                    listAfterwordsPayment.Remove(data);
            }
            else
            {
                dispinfoSelectAfterwordsPaymentData.DbDelete(new DbConnection());
                listAfterwordsPayment.Remove(dispinfoSelectAfterwordsPaymentData);
            }

            // データ更新してDataGridを再表示
            dispctrlDataGridRefreshAfterwordsPayment = true;
            SwitchLayout(0);
        }

        private void dgridAfterwordsPayment_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                menuitemDeleteRow_Click(null, null);
        }

        private void OnFilterToggleButtonClick(object sender, RoutedEventArgs e)
        {
            string[] arrCode = { "201", "208", "102",  "204", "207", "232", "231", "203" };
            string[] arrName = { "JCB", "法人JCB", "口座", "京王", "セゾン", "Pal", "大地", "ヨドバシ" };
            ToggleButton senderButton = sender as ToggleButton;

            if (senderButton == null)
                return;

            string ButtonLabel = senderButton.Content.ToString();

            // StackPanel内のToggleButtonを取得
            List<ToggleButton> list = CommonMethod.FindVisualChild<ToggleButton>(lstack, "ToggleButton");

            // ToggleButtonの押下状態を変更する
            //   全ての場合は全て以外はOFF状態
            //   全て意外はsenderと同じ（押下されたTButton）ではないのは全てOFF状態
            foreach (ToggleButton tbtn in list)
            {
                if (ButtonLabel.Equals("全て"))
                {
                    if (!tbtn.Content.Equals("全て"))
                        tbtn.IsChecked = false;
                }
                else
                {
                    if (ButtonLabel.Equals(tbtn.Content))
                        tbtn.IsChecked = true;
                    else
                        tbtn.IsChecked = false;
                }
            }
            if (ButtonLabel.Equals("全て"))
                dispinfoFilterCreditCode = "";
            else
            {
                int idx = 0;
                for (idx = 0; idx < arrCode.Length; idx++ )
                {
                    if (ButtonLabel.Equals(arrName[idx]))

                        break;
                }

                dispinfoFilterCreditCode = arrCode[idx];
            }

            SwitchLayout(0);

            // 各行の更新フラグをクリアする
            //   OBJECT生成後、行の描画時に自動的にプロパティ更新が走るので、一度明示的にフラグをクリアする
            foreach (AfterwordsPaymentData data in ColViewListAfterwordsPayment.Cast<AfterwordsPaymentData>())
                data.IsPropertyUpdate = false;

        }

        private void btnAutoSetting_Click(object sender, RoutedEventArgs e)
        {
            if (dispinfoFilterCreditCode.Length <= 0)
            {
                MessageBox.Show("貸方種類が選択されていません");
                return;
            }

            //foreach (AfterwordsPaymentData data in ColViewListAfterwordsPayment.Cast<AfterwordsPaymentData>())
            foreach (AfterwordsPaymentData data in dgridAfterwordsPayment.SelectedItems)
            {
                DateTime workDt, lastTimeOrg;
                if (data.LastTimePaymentDate == null)
                    continue;

                lastTimeOrg = (DateTime)data.LastTimePaymentDate;
                workDt = lastTimeOrg.AddDays(1);

                bool IsEndOfMonth = false;
                if (workDt.Month != lastTimeOrg.Month)
                    IsEndOfMonth = true;

                if (IsEndOfMonth)
                {
                    int month = lastTimeOrg.Month;
                    DateTime dt = lastTimeOrg.AddMonths(1);
                    if (month == 2 || month == 4 || month == 6 || month == 9 || month == 11)
                        data.DecisionDate = dt.AddDays(1);
                    else
                        data.DecisionDate = dt;
                }
                else
                {
                    DateTime dt = lastTimeOrg.AddMonths(1);
                    data.DecisionDate = dt;
                }
            }
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dispinfoFilterCreditCode.Length <= 0)
            {
                MessageBox.Show("貸方種類が選択されていません");
                return;
            }

            int count = 0;
            DbConnection dbcon = new DbConnection();
            dbcon.BeginTransaction("ROWS_UPDATE");

            foreach (AfterwordsPaymentData data in ColViewListAfterwordsPayment.Cast<AfterwordsPaymentData>())
            {
                if (!data.IsPropertyUpdate)
                    continue;

                data.DbUpdate(dbcon);
                count++;
            }

            dbcon.CommitTransaction();

            if (count > 0)
                MessageBox.Show(count + "件更新しました");
        }

        private void btnManyDateRegist_Click(object sender, RoutedEventArgs e)
        {
            List<TextBox> listTextBox = CommonMethod.FindVisualChild<TextBox>(lgridEditEachDate, "TextBox");
            List<AfterwordsPaymentData> listInputData = new List<AfterwordsPaymentData>();

            DbConnection dbcon = new DbConnection();
            try
            {
                for (int idx = 0; idx < listTextBox.Count; idx = idx + 3)
                {
                    AfterwordsPaymentData data = CheckText(listTextBox[idx].Text, listTextBox[idx + 1].Text, listTextBox[idx + 2].Text);

                    if (data != null)
                    {
                        data.DebitCode = dispinfoSelectAfterwordsPaymentData.DebitCode;
                        data.CreditCode = dispinfoSelectAfterwordsPaymentData.CreditCode;
                        listInputData.Add(data);
                    }
                }

                // トランザクションの開始
                dbcon.BeginTransaction("AFTERWORDSREGIST");

                Payment.RegistDecisionFromAfterwordsPayment(listInputData, account, dbcon);

                // 選択行の前回支払日をシステム日付で更新
                dispinfoSelectAfterwordsPaymentData.LastTimePaymentDate = DateTime.Now;
                dispinfoSelectAfterwordsPaymentData.DbUpdate(dbcon);

                dbcon.CommitTransaction();
            }
            catch(Exception exp)
            {
                Debug.Write(exp);
                dbcon.RollbackTransaction();
                MessageBox.Show(exp.Message);
                return;
            }

            // 登録が正常に終了した場合はテキストボックスを全てクリア
            foreach (TextBox textbox in listTextBox)
                textbox.Text = "";

            dispctrlDataGridRefreshDecision = true;

            // lgridEditEachDateをCloseしてDataGridを表示
            SwitchLayout(0);
        }

        private AfterwordsPaymentData CheckText(string myDate, string myAmount, string myRemark)
        {
            DateTime dt;
            try
            {
                dt = Convert.ToDateTime(myDate);
            }
            catch(Exception)
            {
                return null;
            }
            long amount;
            try
            {
                amount = Convert.ToInt64(myAmount);
            }
            catch (Exception)
            {
                return null;
            }

            AfterwordsPaymentData data = new AfterwordsPaymentData();

            data.DecisionDate = dt;
            data.Amount = amount;
            data.Remark = myRemark;

            return data;
        }

        private void OnManyDateInputDate_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox activeTextBox = sender as TextBox;

            List<TextBox> listTextBox = CommonMethod.FindVisualChild<TextBox>(lgridEditEachDate, "TextBox");
            for (int idx = 0; idx < listTextBox.Count; idx = idx + 3)
            {
                if (activeTextBox == listTextBox[idx])
                {
                    //Debug.Print("ROW [" + idx + "]");
                    if (dispinfoSelectAfterwordsPaymentData == null)
                        break;

                    if (listTextBox[idx].Text.Length > 0)
                        listTextBox[idx+1].Text = Convert.ToString(dispinfoSelectAfterwordsPaymentData.Amount);

                    if (listTextBox[idx].Text.Length > 0)
                        listTextBox[idx+2].Text = dispinfoSelectAfterwordsPaymentData.Remark;
                }
            }
        }

        private void menuitemDecisionInput_Click(object sender, RoutedEventArgs e)
        {
            SwitchLayout(LAYOUTMODE_EACHDATE);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (dispinfoFilterCreditCode.Length > 0 && lgridPaymentDecision.Visibility == System.Windows.Visibility.Visible)
            {
                env.SetCardDecision(dispinfoFilterCreditCode, dtpickerDecisionSchedule.SelectedDate, dispinfoData.CardDecisionAmount);
            }
        }


        private void btnDecisionDataRegist_Click(object sender, RoutedEventArgs e)
        {
            List<TextBox> listTextBox = CommonMethod.FindVisualChild<TextBox>(lgridEditEachDate, "TextBox");
            List<AfterwordsPaymentData> listInputData = dispinfoSelectListAfterwordsPaymentData;

            DbConnection dbcon = new DbConnection();
            try
            {
                if (listInputData == null)
                {
                    if (dispinfoSelectAfterwordsPaymentData == null)
                    {
                        MessageBox.Show("確定データ登録する対象のデータを選択して下さい", "チェック");
                        return;
                    }
                    listInputData = new List<AfterwordsPaymentData>();
                    listInputData.Add(dispinfoSelectAfterwordsPaymentData);
                }

                // トランザクションの開始
                dbcon.BeginTransaction("AFTERWORDSREGIST");

                Payment.RegistDecisionFromAfterwordsPayment(listInputData, account, dbcon);

                bool IsDelete = false;
                foreach (AfterwordsPaymentData data in listInputData)
                {
                    if (data.Kind == AfterwordsPaymentData.KIND_TEMPORARY)
                    {
                        data.DbDelete(dbcon);
                        IsDelete = true;
                        continue;
                    }
                    data.LastTimePaymentDate = data.DecisionDate;
                    data.DisplayDecisionDate = "";
                    data.DbUpdate(dbcon);
                }

                dbcon.CommitTransaction();

                if (IsDelete)
                {
                    listAfterwordsPayment = AfterwordsPayment.GetData();
                    dgridAfterwordsPayment.ItemsSource = listAfterwordsPayment;
                    dgridAfterwordsPayment.Items.Refresh();
                }
            }
            catch (Exception exp)
            {
                Debug.Write(exp);
                dbcon.RollbackTransaction();
                MessageBox.Show(exp.Message);
                return;
            }

            // 次回表示時にはデータベースから再取得する
            dispctrlDataGridRefreshDecision = true;

            // 親画面の表示データ更新
            ((MainWindow)this.Owner).OwnerRefreshDisplayData();
        }

        private void btnDecisionExecute_Click(object sender, RoutedEventArgs e)
        {
            // 確定対象となるトグルボタンが選択されていること
            if (dispinfoFilterCreditCode == null || dispinfoFilterCreditCode.Length <= 0)
            {
                MessageBox.Show("確定対象となる借入を選択して下さい", "確定前チェック");
                return;
            }

            if (dtpickerDecision.SelectedDate == null)
            {
                MessageBox.Show("支払日を選択して下さい", "確定前チェック");
                return;
            }

            // 確認としてメッセージボックスに表示する文字列を生成
            //     日付（支払予定日）
            //     確定対象借入
            string message;
            DateTime paymentDate = (DateTime)dtpickerDecision.SelectedDate;
            message = "以下の条件で確定処理を行います\n\n"
                        + "　　　支払予定日：" + paymentDate.ToString("yyyy/MM/dd") + "\n"
                        + "　　　 確定対象 ：" + dispinfoFilterCreditCode + "00" + "\n\n";

            MessageBoxResult result = MessageBox.Show(message, "確定前チェック", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
                return;

            // OBJデータベース生成
            DbConnection myDbCon = new DbConnection();

            try
            {
                //////////////////////////////////////////////////
                // データグリッド上にはなく、借入集計が未精算の //
                // 借入取引コードをリストへ追加する             //
                //////////////////////////////////////////////////
                List<string> listLoanDealingCode = Loan.GetListLoanDealingCode(dispinfoFilterCreditCode + "00");

                ////////////////////////////
                // トランザクションの開始 //
                ////////////////////////////
                myDbCon.BeginTransaction("LOANDECISION");

                ////////////////////////////////
                // データグリッド上の借入処理 //
                ////////////////////////////////
                foreach(string loanDealingCode in listLoanDealingCode)
                {
                    foreach(PaymentData data in dispinfoSelectListDecisionData)
                    {
                        if (!data.CreditCode.Equals(loanDealingCode))
                            continue;

                        // 借入確定
                        //   loanDealingCode毎に1回のLoan処理
                        //     引数にList型dispinfoSelectListDecisionDataを渡しているので、一度実行するとbreakして次のコード
                        Loan loan = new Loan(loanDealingCode, paymentDate, dispinfoSelectListDecisionData, myDbCon);

                        // 借入確定（借入集計にのみ存在するデータの確定）
                        loan = new Loan(loanDealingCode, paymentDate, dispinfoSelectListDecisionData, 0, myDbCon);

                        break;
                    }
                }

                // データベースの更新をコミット
                myDbCon.CommitTransaction();
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
                myDbCon.RollbackTransaction();
                MessageBox.Show(ex.Message);
                return;
            }

            // 親画面の表示データ更新
            ((MainWindow)this.Owner).OwnerRefreshDisplayData();

            dispctrlDataGridRefreshDecision = true;
            SwitchLayout(LAYOUTMODE_DATAGRID_DECISION);

            env.SetCardDecision(dispinfoFilterCreditCode, null, 0);
            dtpickerDecision.SelectedDate = null;
            txtbEnvAmount.Text = "";
        }

        private void txtCardDecisionAmount_LostFocus(object sender, RoutedEventArgs e)
        {
            dgridAfterwordsPayment_SelectionChanged(null, null);
        }

        private void txtbCardDifference_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dispinfoIsTotalDisplayCardDifference)
                dispinfoIsTotalDisplayCardDifference = false;
            else
                dispinfoIsTotalDisplayCardDifference = true;

            dgridAfterwordsPayment_SelectionChanged(null, null);
        }

        private void dgridDecision_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                // 削除対象のデータ確認
                bool IsDecided = false;
                if (dgridDecision.SelectedItems.Count > 1)
                {
                    foreach (PaymentData data in dgridDecision.SelectedItems)
                    {
                        if (data.PaymentDate.Year >= 2000)
                        {
                            IsDecided = true;
                            break;
                        }
                    }
                }
                else if (dgridDecision.SelectedItems.Count == 1)
                {
                    PaymentData seldata = (PaymentData)dgridDecision.SelectedItem;
                    if (seldata.PaymentDate.Year >= 2000)
                        IsDecided = true;
                }
                if (IsDecided)
                {
                    MessageBox.Show("確定済みのデータを削除することは出来ません", "削除前データ確認");
                    return;
                }

                MessageBoxResult result = MessageBox.Show("削除して宜しいですか？", "削除確認", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.Cancel)
                    return;

                DbConnection dbcon = new DbConnection();
                dbcon.BeginTransaction("ROWS_DELETE");

                if (dgridDecision.SelectedItems.Count > 1)
                {
                    foreach (PaymentData data in dgridDecision.SelectedItems)
                    {
                        data.DbDeleteLoanDetail(dbcon);
                        listDecision.Remove(data);
                    }
                }
                else if (dgridDecision.SelectedItems.Count == 1)
                {
                    PaymentData seldata = (PaymentData)dgridDecision.SelectedItem;
                    seldata.DbDeleteLoanDetail(dbcon);
                    listDecision.Remove(seldata);
                }

                dbcon.CommitTransaction();

                dispctrlDataGridRefreshDecision = true;
                SwitchLayout(0);
            }
        }
    }
}
