using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.Data.SqlClient;
using NLog;
using System.ComponentModel;

namespace wpfHouseholdAccounts
{
    /// <summary>
    /// winMoneyInput.xaml の相互作用ロジック
    /// </summary>
    public partial class winMoneyInput : Window
    {
        public readonly static RoutedCommand Calculate = new RoutedCommand("Calculate", typeof(winMoneyInput));

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        DbConnection dbcon;
        Account account;
        Account accountDetail;
        MoneyNowParent parentNowInfo;
        Environment env;
        List<AccountData> listAccount;
        List<AccountData> listAccountDetail;
        List<PaymentData> listPaymentDecision; // 画面：支払確定情報
        private bool _isTabPressed;

        private int dispinfoBeforeGridRowId = 0;
        private DateTime dispinfoRegistDate;
        private DateTime dispinfoDecisionBaseDate;

        public winMoneyInput()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CommandBindings.Add(new CommandBinding(Calculate, (s, ea) => { btnCalculate_Click(s, ea); }, (s, ea) => ea.CanExecute = true));

            env = new Environment();
            dbcon = new DbConnection();
            account = new Account();
            accountDetail = new Account();

            // 環境情報を取得、画面に反映
            SetEnvironmentInfo();

            // XMLから保存データを取得（存在する場合のみ）
            string XmlFileName = env.GetXmlSavePathname("XmlMoneyInputFileName");
            dgridMoneyInput.ItemsSource = MoneyInput.ImportXml(XmlFileName);

            // リストボックスへ費用項目の表示
            Binding items = new Binding();

            items.Source = account.GetTwoDigitItems();
            items.Path = new PropertyPath(".");

            lstAccountExpense.SetBinding(ItemsControl.ItemsSourceProperty, items);

            // 支払確定情報の取得、画面に反映
            listPaymentDecision = Payment.GridvDecisionPaymentSetData();
            dgridPaymentSchedule.ItemsSource = listPaymentDecision;

            listAccountDetail = accountDetail.GetItems();
            lstAccountDetail.ItemsSource = listAccountDetail;

            parentNowInfo = new MoneyNowParent();
            parentNowInfo.SetInfo(accountDetail);
            parentNowInfo.Calculate(dispinfoRegistDate, (DateTime)dtpickDecisionBaseDate.SelectedDate, listPaymentDecision);

            // 現在情報の表示
            dgridNowInfo.ItemsSource = parentNowInfo.listMoneyNowData;

            // 支払確定の各行の色を設定
            SetRowColorPaymentDesicion();
        }

        private void SetEnvironmentInfo()
        {
            // 支払確定基準日の取得、設定
            string BaseDate = env.GetValue("支払確定集計基準日");
            if (BaseDate.Length > 0)
                dtpickDecisionBaseDate.Text = BaseDate;

            // 現在現金金額
            txtCashNow.Text = env.GetValue("現在現金金額");
            // 現在現金金額
            txtCashCompanyNow.Text = env.GetValue("現在現金会社金額");

            string registDate = env.GetValue("金銭帳入力登録日");
            if (registDate.Length <= 0)
                calendarRegistDate.SelectedDate = DateTime.Now;
            else
                calendarRegistDate.SelectedDate = Convert.ToDateTime(registDate);

        }

        /// <summary>
        /// 支払確定の各行の色を設定する
        /// </summary>
        private void SetRowColorPaymentDesicion()
        {
            if (dgridPaymentSchedule.ItemsSource == null)
                return;
            // 支払確定の色情報の設定
            foreach (PaymentData data in dgridPaymentSchedule.ItemsSource)
            {
                // なぜかUpdateLayoutを実行しないとrowの戻りがnullになる
                // http://stackoverflow.com/questions/6713365/itemcontainergenerator-containerfromitem-returns-null EverClip
                dgridPaymentSchedule.UpdateLayout();
                var row = dgridPaymentSchedule.ItemContainerGenerator.ContainerFromItem(data) as DataGridRow;
                // 過去の支払確定の行はLightGrayで設定
                if (data.PaymentDate.CompareTo(dispinfoRegistDate) < 0)
                {
                    if (row != null)
                        row.Background = Brushes.LightGray;
                }
                // 同じ日の場合は取込済かにより色を変える
                if (data.PaymentDate.CompareTo(dispinfoRegistDate) == 0)
                {
                    List<MoneyInputData> inputdata = (List<MoneyInputData>)dgridMoneyInput.ItemsSource;

                    if (MoneyInput.IsImported(data, inputdata))
                    {
                        if (row != null) row.Background = Brushes.LightSlateGray;
                        data.IsCapture = true;
                    }
                    else
                        if (row != null)  row.Background = Brushes.LemonChiffon;
                }
            }

        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                DateTime dt = Convert.ToDateTime(dispinfoRegistDate);
                env.SetData("金銭帳入力登録日", dt.ToShortDateString());
                env.SetData("現在現金金額", txtCashNow.Text);
                env.SetData("現在現金会社金額", txtCashCompanyNow.Text);

                string XmlFileName = env.GetXmlSavePathname("XmlMoneyInputFileName");

                List<MoneyInputData> inputdata = (List<MoneyInputData>)dgridMoneyInput.ItemsSource;
                MoneyInput.ExportXml(XmlFileName, inputdata);

                // XMLからデータを保存
                XmlFileName = env.GetXmlSavePathname("XmlMoneyInputSaveDataFileName");

                List<MoneyInputData> savedata = (List<MoneyInputData>)dgridSaveData.ItemsSource;
                MoneyInput.ExportXml(XmlFileName, savedata);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Window_Closing ");
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        private void btnRegist_Click(object sender, RoutedEventArgs e)
        {
            List<MoneyInputData> listInputData = (List<MoneyInputData>)dgridMoneyInput.ItemsSource;

            if (listInputData == null || listInputData.Count <= 0)
            {
                MessageBox.Show("登録対象のデーターが存在しません");
                return;
            }
            try
            {
                // 入力チェック 入力途中の行は対象としない
                //   科目コードが全て有効であることの確認
                //   摘要必須の項目チェック
                MoneyInput.CheckData(listInputData, account);

                List<PaymentData> paydata = (List<PaymentData>)dgridPaymentSchedule.ItemsSource;

                // 支払確定の取込漏れチェック
                Payment.CheckImported(dispinfoRegistDate, paydata, listInputData);

                MoneyNowData dataCash = parentNowInfo.GetCash();
                MoneyNowData dataCashCompany = parentNowInfo.GetCashExpenseCampany();

                MoneyInputRegist reg = new MoneyInputRegist(listInputData, account, dbcon);
                DateTime dtReg = Convert.ToDateTime(dispinfoRegistDate);

                // 入力した内容をXmlファイル名のSuffixに日付を付けて保存する
                List<MoneyInputData> inputdata = (List<MoneyInputData>)dgridMoneyInput.ItemsSource;
                MoneyInput.SaveXml(env, dtReg, inputdata);

                // 登録処理の実行
                //   DBのトランザクションは同メソッド内で完結
                reg.Execute(dtReg, dataCash, dataCashCompany);

                listInputData.Clear();
                dgridMoneyInput.Items.Refresh();

                DateTime dt = dispinfoRegistDate.AddDays(1);
                calendarRegistDate.SelectedDate = dt;

                // 現在情報を更新
                parentNowInfo.SetInfo(accountDetail);
                parentNowInfo.Calculate(dispinfoRegistDate, (DateTime)dtpickDecisionBaseDate.SelectedDate, listPaymentDecision);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "btnRegist_Click ");
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);

                MessageBox.Show("エラーが発生" + ex.Message);
            }

            // 親画面の表示データ更新
            ((MainWindow)this.Owner).OwnerRefreshDisplayData();
        }

        private void dgridMoneyInput_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            try
            {
                // 新規追加行の場合は何もしない
                if (dgridMoneyInput.CurrentCell.Item == CollectionView.NewItemPlaceholder)
                    return;

                DataGridCell cell = e.OldFocus as DataGridCell;
                MoneyInputData data = dgridMoneyInput.CurrentCell.Item as MoneyInputData;
                object obj = dgridMoneyInput.CurrentItem;

                if (cell == null)
                {
                    if (e.OldFocus is TextBox)
                    {
                        var txtbox = e.OldFocus as TextBox;
                        cell = txtbox.Parent as DataGridCell;

                        if (cell.Column.Header.Equals("借CD")
                                || cell.Column.Header.Equals("貸CD"))
                        {
                            if (cell.Column.Header.Equals("借CD"))
                                data.DebitCode = txtbox.Text;
                            else if (cell.Column.Header.Equals("貸CD"))
                                data.CreditCode = txtbox.Text;

                            lstAccountExpense.Visibility = System.Windows.Visibility.Collapsed;
                            lstAccountDetail.Visibility = System.Windows.Visibility.Collapsed;
                        }
                        else if (cell.Column.Header.Equals("日のみ"))
                        {
                            if (txtbox.Text.Length > 0)
                            {
                                int nowday = DateTime.Now.Day;
                                int inputday = Convert.ToInt32(txtbox.Text);

                                try
                                {
                                    if (inputday > nowday)
                                        data.Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month - 1, inputday);
                                    else
                                        data.Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, inputday);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    txtbox.Text = "";
                                }
                            }
                            return;
                        }
                        else if (cell.Column.Header.Equals("年月日"))
                        {
                            // 直接年月日に入力した場合は表示用から格納年月日へ変換する
                            if (txtbox.Text.Length > 0)
                            {
                                DateTime dt;
                                try
                                {
                                    dt = Convert.ToDateTime(txtbox.Text);
                                }
                                catch (FormatException)
                                {
                                    return;
                                }

                                data.Date = dt;
                            }
                            return;
                        }
                    }
                }

                if (cell == null)
                {
                    // 何もしない、後続のelse ifを実行させない
                    Debug.Print("行の新規作成");
                }
                else if (data.id == dispinfoBeforeGridRowId)
                {
                    Debug.Print("dgridMoneyInput_LostKeyboardFocus [" + cell.Column.Header + "]  data.id [" + data.id + "]   dispinfoBeforeGridRowId [" + dispinfoBeforeGridRowId + "]");
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
                }

                dispinfoBeforeGridRowId = data.id;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("dgridMoneyInput_LostKeyboardFocus ", ex);
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        private void dgridMoneyInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //Debug.Print("PreviewKeyDown " + e.Key);
            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                _isTabPressed = true;
                //Debug.Print("PreviewKeyDown TAB or Enter");
            }
        }

        private void dgridMoneyInput_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            try
            {
                Debug.Print("RowEditEnding");

                // http://blogs.msdn.com/b/vinsibal/archive/2009/04/14/5-more-random-gotchas-with-the-wpf-datagrid.aspx
                // リターンキーを押下した場合は、FocusとSelectCellsが違って、入力しにくくなるので
                // リターンキーでもフォーカス、選択セルが移動するように対応
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    // タブ、エンターキーによる、かつ同イベントの移動元が前の行である場合
                    if (_isTabPressed &&
                        e.Row.Item == dgridMoneyInput.Items[dgridMoneyInput.Items.Count - 2])
                    {
                        int colIndex = 0;
                        var rowToSelect = dgridMoneyInput.Items[dgridMoneyInput.Items.Count - 1];
                        var colToSelect = dgridMoneyInput.Columns[colIndex];
                        int rowIndex = dgridMoneyInput.Items.IndexOf(rowToSelect);

                        // リターンキーからの同イベントの場合は、別スレッドでFocusを行うので
                        // FocusとSelectCellsが違ってしまう
                        // そのため、スレッド内で同処理を行うのでコメント化（URLのオリジナルとの違い）
                        //dgridMoneyInput.SelectedCells.Clear();
                        //dgridMoneyInput.SelectedCells.Add(
                        //        new DataGridCellInfo(rowToSelect, colToSelect));

                        this.Dispatcher.BeginInvoke(new DispatcherOperationCallback((param) =>
                        {
//                            System.Threading.Thread.Sleep(500);
                            var cell = DataGridHelper.GetCell(dgridMoneyInput, rowIndex, colIndex);
                            // クリアしないと２つのセルが選択された状態になる
                            dgridMoneyInput.SelectedCells.Clear();
                            cell.Focus();
                            cell.IsSelected = true;

                            //dgridMoneyInput.BeginEdit();
                            return null;
                        }), DispatcherPriority.Background, new object[] { null });
                    }
                    _isTabPressed = false;
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("dgridMoneyInput_RowEditEnding ", ex);
                Debug.Print(ex.Message);
                Debug.Write(ex);
            }

            // 現金の差引金額を更新
            OnTextboxCashBothNow_TextChanged(null, null);
        }

        /// <summary>
        /// 初期画面を開いた時の借方CDにマウスのダブルクリックした場合に「10100：現金」が自動入力されてしまう
        /// 原因はスレッドでLostFocusが２回？起動しているためっぽいが対応として以下のイベントクリアする
        /// 借方名が変わらないのは取りあえずスルーする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgridMoneyInput_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            Debug.Print("dgridMoneyInput_PreparingCellForEdit");

            var txtbox = e.EditingElement as TextBox;

            DataGridCell cell = txtbox.Parent as DataGridCell;

            if (cell == null)
                return;

            Debug.Print("dgridMoneyInput_PreparingCellForEdit [" + cell.Column.Header + "] DATA [" + txtbox.Text + "]");
            if (txtbox.Text.Equals(Account.CODE_CASH))
                txtbox.Text = "";
        }

        private void dgridMoneyInput_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Debug.Print("GotKeyboardFocus [" + e.NewFocus.GetType() + "]");
            if (e.NewFocus is TextBox)
            {
                var txtbox = e.NewFocus as TextBox;

                DataGridCell cell = txtbox.Parent as DataGridCell;

                if (cell == null)
                    return;

                Debug.Print("GotKeyboardFocus cell.Column.Header [" + cell.Column.Header + "]");
                if (cell.Column.Header.Equals("借CD") || cell.Column.Header.Equals("貸CD"))
                {
                    lstAccountExpense.Visibility = System.Windows.Visibility.Visible;
                    lstAccountDetail.Visibility = System.Windows.Visibility.Visible;

                    ICollectionView view = CollectionViewSource.GetDefaultView(listAccountDetail);

                    new TextSearchFilterForGrid(view, txtbox);
                }
                txtbox.Focus();
                txtbox.Select(0, txtbox.Text.Length);
            }
        }

        private void btnDisplayControlSaveData_Click(object sender, RoutedEventArgs e)
        {
            Visibility visi = lgridSaveData.Visibility;

            string XmlFileName = "";
            if (visi == System.Windows.Visibility.Visible)
            {
                lgridSaveData.Visibility = System.Windows.Visibility.Hidden;

                // XMLからデータを保存
                XmlFileName = env.GetXmlSavePathname("XmlMoneyInputSaveDataFileName");

                List<MoneyInputData> savedata = (List<MoneyInputData>)dgridSaveData.ItemsSource;
                MoneyInput.ExportXml(XmlFileName, savedata);

                return;
            }

            // XMLから保存データを取得（存在する場合のみ）
            XmlFileName = env.GetXmlSavePathname("XmlMoneyInputSaveDataFileName");

            if (!System.IO.File.Exists(XmlFileName))
            {
                Debug.Print("XmlMoneyInputSaveDataFileName [" + XmlFileName + "] not found. create");
                _logger.Debug("XmlMoneyInputSaveDataFileName [" + XmlFileName + "] not found. create");

                List<MoneyInputData> savedata = (List<MoneyInputData>)dgridSaveData.ItemsSource;
                MoneyInput.ExportXml(XmlFileName, savedata);
            }

            dgridSaveData.ItemsSource = MoneyInput.ImportXml(XmlFileName);
            lgridSaveData.Visibility = System.Windows.Visibility.Visible;
        }

        private void btnSaveData_Click(object sender, RoutedEventArgs e)
        {
            if (dgridMoneyInput.SelectedItems.Count <= 0)
                MessageBox.Show("Saveするデータを選択して下さい");

            try
            {
                List<MoneyInputData> listSaveData = (List<MoneyInputData>)dgridSaveData.ItemsSource;

                var selData = dgridMoneyInput.SelectedItems;

                foreach (MoneyInputData data in selData)
                    listSaveData.Add(data);

                dgridSaveData.Items.Refresh();

                List<MoneyInputData> listData = (List<MoneyInputData>)dgridMoneyInput.ItemsSource;
                foreach (MoneyInputData data in selData)
                    listData.Remove(data);

                dgridMoneyInput.Items.Refresh();
            }
            catch(Exception ex)
            {
                _logger.ErrorException("btnSaveData_Click ", ex);
                MessageBox.Show("Saveでエラーが発生しました [" + ex.Message + "]");
            }

            return;
        }

        private void btnRestoreData_Click(object sender, RoutedEventArgs e)
        {
            if (dgridSaveData.SelectedItems.Count <= 0)
                MessageBox.Show("Restoreするデータを選択して下さい");

            try
            {
                List<MoneyInputData> listData = (List<MoneyInputData>)dgridMoneyInput.ItemsSource;

                var selData = dgridSaveData.SelectedItems;

                foreach (MoneyInputData data in selData)
                    listData.Add(data);

                dgridMoneyInput.Items.Refresh();

                List<MoneyInputData> listSaveData = (List<MoneyInputData>)dgridSaveData.ItemsSource;

                foreach (MoneyInputData data in selData)
                    listSaveData.Remove(data);

                dgridSaveData.Items.Refresh();
            }
            catch(Exception ex)
            {
                _logger.ErrorException("btnRestoreData_Click ", ex);
                MessageBox.Show("Restoreでエラーが発生しました [" + ex.Message + "]");
            }
        }

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            List<MoneyInputData> listInputdata = (List<MoneyInputData>)dgridMoneyInput.ItemsSource;

            parentNowInfo.SummaryDebitCredit(listInputdata, account);

            parentNowInfo.Calculate(dispinfoRegistDate, (DateTime)dtpickDecisionBaseDate.SelectedDate, listPaymentDecision);

            List<MoneyInputData> listData = (List<MoneyInputData>)dgridMoneyInput.ItemsSource;

            foreach(MoneyInputData inputdata in listData)
            {
                inputdata.DebitName = account.getName(inputdata.DebitCode);
                inputdata.CreditName = account.getName(inputdata.CreditCode);
            }

            OnTextboxCashBothNow_TextChanged(null, null);
        }

        private void btnImportPaymentDecision_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<MoneyInputData> listData = (List<MoneyInputData>)dgridMoneyInput.ItemsSource;

                var paydata = (List<PaymentData>)dgridPaymentSchedule.ItemsSource;

                DateTime dtRegistDate = Convert.ToDateTime(dispinfoRegistDate);
                foreach (PaymentData data in paydata)
                {
                    if (dtRegistDate.CompareTo(data.PaymentDate) == 0)
                    {
                        MoneyInputData inputdata = new MoneyInputData();
                        inputdata.DebitCode = data.DebitCode;
                        inputdata.DebitName = account.getName(data.DebitCode);
                        inputdata.CreditCode = data.CreditCode;
                        inputdata.CreditName = account.getName(data.CreditCode);
                        inputdata.Amount = data.Amount;
                        inputdata.Remark = data.Remark;

                        listData.Add(inputdata);

                        data.IsCapture = true;
                    }
                }

                dgridMoneyInput.Items.Refresh();

                SetRowColorPaymentDesicion();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("btnImportPaymentDecision_Click ", ex);
                MessageBox.Show("Restoreでエラーが発生しました [" + ex.Message + "]");
            }
        }

        // OnTextboxCashNow_TextChanged
        private void OnTextboxCashBothNow_TextChanged(object sender, TextChangedEventArgs e)
        {
            List<MoneyInputData> listInputdata = (List<MoneyInputData>)dgridMoneyInput.ItemsSource;

            if (listInputdata == null)
                return;

            parentNowInfo.SummaryDebitCredit(listInputdata, account);

            MoneyNowData dataCash = parentNowInfo.GetCash();
            MoneyNowData dataCashCompany = parentNowInfo.GetCashExpenseCampany();

            long haveCash = 0;
            long haveCashCompany = 0;
            try
            {
                haveCash = Convert.ToInt64(txtCashNow.Text);
                haveCashCompany = Convert.ToInt64(txtCashCompanyNow.Text);
            }
            catch (Exception)
            {
                // 何もしない
            }

            txtCashBalance.Text = Convert.ToString(dataCash.HaveCashAmount - haveCash);
            txtCashCompanyBalance.Text = Convert.ToString(dataCashCompany.HaveCashAmount - haveCashCompany);
        }


        private void dtpickRegistDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // 支払確定の各行の色を設定
            SetRowColorPaymentDesicion();
        }

        private void calendarRegistDate_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            dispinfoRegistDate = (DateTime)calendarRegistDate.SelectedDate;

            // 支払確定の各行の色を設定
            SetRowColorPaymentDesicion();
        }

        private void dtpickDecisionBaseDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (accountDetail == null)
                return;

            dispinfoDecisionBaseDate = (DateTime)dtpickDecisionBaseDate.SelectedDate;

            // 支払確定基準日の取得、設定
            env.SetData("支払確定集計基準日", dispinfoDecisionBaseDate.ToString("yyyy/MM/dd"));

            if (parentNowInfo != null)
            {
                // 現在情報を更新
                parentNowInfo.SetInfo(accountDetail);
                parentNowInfo.Calculate(dispinfoRegistDate, (DateTime)dtpickDecisionBaseDate.SelectedDate, listPaymentDecision);
            }
        }

        private void dgridMoneyInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            long seltotal = 0;

            if (dgridMoneyInput.SelectedItems.Count > 1)
            {
                foreach (object o in dgridMoneyInput.SelectedItems)
                {
                    MoneyInputData data = o as MoneyInputData;
                    try
                    {
                        data = o as MoneyInputData;
                    }
                    catch(InvalidCastException ex)
                    {
                        Debug.Write(ex);
                        _logger.ErrorException("dgridMoneyInput_SelectionChanged ", ex);
                    }
                    if (data != null)
                        seltotal += data.Amount;

                }
            }

            txtbSelectedAddup.Text = String.Format("選択合計 \\"+ "{0:###,###,##0}", seltotal);
        }
    }
}
