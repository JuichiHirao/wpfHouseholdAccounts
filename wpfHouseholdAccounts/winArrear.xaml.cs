using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using wpfHouseholdAccounts.arrear;

namespace wpfHouseholdAccounts
{
    /// <summary>
    /// winArrear.xaml の相互作用ロジック
    /// </summary>
    public partial class winArrear : Window
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        DbConnection dbcon;
        Account account;
        Account accountDetail;
        Environment env;
        Arrears arrears;

        List<AccountData> listAccount;
        List<AccountData> listAccountDetail;
        List<arrear.TargetAccountData> listTargetAccountData;
        List<ArrearInputData> listArrearInputData = null;
        List<ArrearInputData> listArrearDbData = null;

        private bool _isTabPressed;

        private int dispinfoBeforeGridRowId = 0;
        private DateTime dispinfoAdjustDate;

        private bool dispinfoIsToggleModeInput;

        private void SetToggleMode(object myContent)
        {
            dispinfoIsToggleModeInput = false;

            if (myContent != null)
            {
                if (myContent.ToString().Equals("入力"))
                    dispinfoIsToggleModeInput = true;
            }
        }
        public winArrear()
        {
            InitializeComponent();

            dbcon = new DbConnection();
            env = new Environment();
            account = new Account();
            accountDetail = new Account();
            arrears = new Arrears();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // リストボックスへ費用項目の表示
            Binding items = new Binding();

            items.Source = account.GetTwoDigitItems();
            items.Path = new PropertyPath(".");

            lstAccountExpense.SetBinding(ItemsControl.ItemsSourceProperty, items);

            // 未払対象の取得、画面に反映
            arrear.TargetAccount targetAccount = new arrear.TargetAccount(dbcon);
            listTargetAccountData = targetAccount.GetList();
            dgridArrearTarget.ItemsSource = listTargetAccountData;

            listAccountDetail = accountDetail.GetItems();
            lstAccountDetail.ItemsSource = listAccountDetail;

            dtpickAdjustDate.SelectedDate = dispinfoAdjustDate  = DateTime.Now;

            listArrearDbData = arrears.GetArrearList(dbcon);
            listArrearInputData = ArrearInput.ImportXml(env);

            SetArrearUiSetting();
        }

        private void OnArrearModeClick(object sender, RoutedEventArgs e)
        {
            SetToggleMode(((ToggleButton)sender).Content);

            SetArrearUiSetting();
        }
        private void SetArrearUiSetting()
        {
            if (dispinfoIsToggleModeInput)
            {
                tbtnModeInput.IsChecked = true;
                tbtnModeControl.IsChecked = false;
                dgridMoneyInput.ItemsSource = listArrearInputData;
                dgridMoneyInput.SelectionMode = DataGridSelectionMode.Extended;
                dgridMoneyInput.SelectionUnit = DataGridSelectionUnit.Cell;
                dgridMoneyInput.CanUserAddRows = true;
                dgridMoneyInput.CanUserDeleteRows = true;
                dgridMoneyInput.Columns[5].Visibility = Visibility.Hidden;
                btnRegister.Content = "登録";
            }
            else
            {
                tbtnModeInput.IsChecked = false;
                tbtnModeControl.IsChecked = true;
                dgridMoneyInput.ItemsSource = listArrearDbData;
                dgridMoneyInput.SelectionUnit = DataGridSelectionUnit.FullRow;
                dgridMoneyInput.CanUserAddRows = false;
                dgridMoneyInput.CanUserDeleteRows = false;
                dgridMoneyInput.Columns[5].Visibility = Visibility.Visible;
                btnRegister.Content = "精算・精算取消";

                SaveInputData("SetArrearUiSetting");
            }

            return;
        }

        private void SaveInputData(string myEventName)
        {
            try
            {
                string XmlFileName = env.GetXmlSavePathname("XmlArrearInputFileName");
                ArrearInput.ExportXml(XmlFileName, listArrearInputData);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, myEventName + " SaveInputData ", ex);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (dispinfoIsToggleModeInput)
                SaveInputData("Window_Closing");
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            List<ArrearInputData> listInputData = (List<ArrearInputData>)dgridMoneyInput.ItemsSource;
            TargetAccountData accountData = (TargetAccountData)dgridArrearTarget.SelectedItem;

            if (listInputData == null || listInputData.Count <= 0)
            {
                MessageBox.Show("登録対象のデーターが存在しません");
                return;
            }
            if (accountData == null)
            {
                MessageBox.Show("未払対象の項目が選択されていません");
                return;
            }
            string name = account.getName(accountData.Code);
            if (name == null || name.Length <= 0)
            {
                MessageBox.Show("選択されている未払項目が不正です");
                return;
            }
            if (!btnRegister.Content.Equals("登録"))
            {
                MessageBox.Show("モードを「入力」に変更して下さい");
                return;
            }

            foreach(ArrearInputData data in (List<ArrearInputData>)dgridMoneyInput.ItemsSource)
            {
                name = account.getName(data.DebitCode);
                if (name == null || name.Length <= 0)
                {
                    MessageBox.Show("登録されていないコード[" + data.DebitCode + "]");
                    return;
                }
                data.ArrearCode = accountData.Code;
            }

            try
            {
                ArrearInput.CheckData(listInputData, account);

                arrears.register(accountData.Code, (List<ArrearInputData>)dgridMoneyInput.ItemsSource, dbcon);

                listInputData.Clear();
                dgridMoneyInput.Items.Refresh();
            }
            catch (BussinessException bex)
            {
                _logger.Error(bex, "btnRegist_Click ");

                MessageBox.Show("エラーが発生" + bex.Message);
            }
            catch (Exception ex)
            {
                dbcon.RollbackTransaction();

                _logger.Error(ex, "btnRegist_Click ");

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
                ArrearInputData data = dgridMoneyInput.CurrentCell.Item as ArrearInputData;
                object obj = dgridMoneyInput.CurrentItem;

                if (cell == null)
                {
                    if (e.OldFocus is TextBox)
                    {
                        var txtbox = e.OldFocus as TextBox;
                        cell = txtbox.Parent as DataGridCell;

                        if (cell.Column.Header.Equals("借CD"))
                        {
                            if (cell.Column.Header.Equals("借CD"))
                                data.DebitCode = txtbox.Text;

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
                    _logger.Debug("行の新規作成");
                }
                else if (data.Id == dispinfoBeforeGridRowId)
                {
                    _logger.Debug("dgridMoneyInput_LostKeyboardFocus [" + cell.Column.Header + "]  data.id [" + data.Id + "]   dispinfoBeforeGridRowId [" + dispinfoBeforeGridRowId + "]");
                    if (cell.Column.Header.Equals("借CD"))
                    {
                        data.DebitName = account.getName(data.DebitCode);
                        _logger.Debug("dgridMoneyInput_LostKeyboardFocus [" + cell.Column.Header + "]  DebitCode [" + data.DebitCode + "]   DebitName [" + data.DebitName + "]");
                    }
                }

                dispinfoBeforeGridRowId = data.Id;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "dgridMoneyInput_LostKeyboardFocus ");
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
                _logger.Debug("RowEditEnding");

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

                if (dispinfoIsToggleModeInput)
                    SaveInputData("dgridMoneyInput_RowEditEnding");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "dgridMoneyInput_RowEditEnding ");
            }
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
            _logger.Debug("dgridMoneyInput_PreparingCellForEdit");

            var txtbox = e.EditingElement as TextBox;

            DataGridCell cell = txtbox.Parent as DataGridCell;

            if (cell == null)
                return;

            _logger.Debug("dgridMoneyInput_PreparingCellForEdit [" + cell.Column.Header + "] DATA [" + txtbox.Text + "]");
            if (txtbox.Text.Equals(Account.CODE_CASH))
                txtbox.Text = "";
        }

        private void dgridMoneyInput_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _logger.Debug("GotKeyboardFocus [" + e.NewFocus.GetType() + "]");
            if (e.NewFocus is TextBox)
            {
                var txtbox = e.NewFocus as TextBox;

                DataGridCell cell = txtbox.Parent as DataGridCell;

                if (cell == null)
                    return;

                _logger.Debug("GotKeyboardFocus cell.Column.Header [" + cell.Column.Header + "]");
                if (cell.Column.Header.Equals("借CD"))
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

        private void dgridMoneyInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            long total = 0;
            long seltotal = 0;

            foreach (object o in dgridMoneyInput.ItemsSource)
            {
                ArrearInputData data = o as ArrearInputData;
                try
                {
                    data = o as ArrearInputData;
                }
                catch (InvalidCastException ex)
                {
                    _logger.Error(ex, "dgridMoneyInput_SelectionChanged ");
                }
                if (data != null)
                    total += data.Amount;

            }

            if (dgridMoneyInput.SelectedItems.Count > 1)
            {
                foreach (object o in dgridMoneyInput.SelectedItems)
                {
                    ArrearInputData data = o as ArrearInputData;
                    try
                    {
                        data = o as ArrearInputData;
                    }
                    catch (InvalidCastException ex)
                    {
                        _logger.Error(ex, "dgridMoneyInput_SelectionChanged ");
                    }
                    if (data != null)
                        seltotal += data.Amount;
                }
            }

            txtbSelectedAddup.Text = String.Format("選択合計 \\" + "{0:###,###,##0} / {0:###,###,##0}", seltotal, total);
        }

        private void dtpickAdjustDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (accountDetail == null)
                return;

            dispinfoAdjustDate = (DateTime)dtpickAdjustDate.SelectedDate;

            // 支払確定基準日の取得、設定
            //env.SetData("支払確定集計基準日", dispinfoAdjustDate.ToString("yyyy/MM/dd"));
        }
    }
}
