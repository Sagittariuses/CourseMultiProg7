using ForntEndMultiprog7.Model;
using FrontEndMultiprog7.Windows;
using LKDSFramework;
using LKDSFramework.Packs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ForntEndMultiprog7.Pages
{
    /// <summary>
    /// Логика взаимодействия для PageMain.xaml
    /// </summary>
    public partial class PageMain : Page
    {
        #region Vars

        #region Int type

        const int PartSize = 928;
        int PageNum;
        int progressValue = 0;
        int CounterDevSubdev = 0;
        
        #endregion

        #region Lkds types

        public static DriverV7 Driver = new DriverV7();
        LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPWriteAns FirmwareLoadPackAns;
        SubDeviceV7 FocusedDev;

        #endregion

        #region Lists

        List<SubDeviceV7> SubDevices = new List<SubDeviceV7>();
        List<byte[]> FirmwareFragments = new List<byte[]>();
        List<string> FwOnPages = new List<string>();

        #endregion

        #region String type
  
        public static string FileExt;
        public static string FileFW;

        private string labelContentUpdate = "Обновление";
        private string labelContentAnalyze = "Анализ";

        private string styleActiveMode = "BtnActiveMode";
        private string styleInactiveMode = "BtnInactiveMode";
        private string styleUpdateDisable = "BtnUpdateEnabled";
        private string styleUpdateEnable = "BtnUpdateEnabled";
        private string selectedStyle;

        #endregion

        #region Byte type

        byte SelectedDevOrSubDevCANID;

        #endregion

        #region Bool type

        bool FWGet = false;
        bool LBCheck = false;
        bool CheckAnalyze = false;
        bool SendLastFragFlag = false;
        bool OnlineActive = true;
        bool OfflineActive = false;
        bool ManualActive = false;
        private bool isEnableUpdate = false;
        bool GoNext = false;

        #endregion

        #region Long type
        
        private long time = 0;

        #endregion

        #region Other types
        Stopwatch stopwatch = new Stopwatch();

        ResourceDictionary Styles = (ResourceDictionary)Application.LoadComponent(
            new Uri("/ForntEndMultiprog7;component/DictionaryStyles/Styles.xaml", UriKind.Relative));

        public ObservableCollection<VMDevice> OcVMDev;

        #endregion

        #endregion

        #region Page Events
        public PageMain()
        {
            InitializeComponent();


        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Driver.OnSubDevChange += Driver_OnSubDevChange;
            Driver.OnReceiveData += Driver_OnReceiveData;
            if (!Driver.Init())
            {
                return;
            }

            if (App.Args.Length != 0)
            {
                try
                {
                    var Devices = DeviceV7.FromArgs(App.Args);
                    Driver.AddDevice(ref Devices[0]);
                    
                }
                catch { }
            }
            OcVMDev = new ObservableCollection<VMDevice>();
            //BindingOperations.EnableCollectionSynchronization(OcVMDev, _lock);
            LVCanDevList.ItemsSource = OcVMDev;
        }

        #endregion

        #region Modes
        private void BtnOnlineMode_Click(object sender, RoutedEventArgs e)
        {
            BtnOnlineMode.Style = (Style)Styles[styleActiveMode];
            BtnOfflineMode.Style = (Style)Styles[styleInactiveMode];
            BtnManualMode.Style = (Style)Styles[styleInactiveMode];

            OnlineActive = true;
            OfflineActive = false;
            ManualActive = false;

        }

        private void BtnOfflineMode_Click(object sender, RoutedEventArgs e)
        {
            BtnOnlineMode.Style = (Style)Styles[styleInactiveMode];
            BtnOfflineMode.Style = (Style)Styles[styleActiveMode];
            BtnManualMode.Style = (Style)Styles[styleInactiveMode];

            OnlineActive = false;
            OfflineActive = true;
            ManualActive = false;
        }

        private void BtnManualMode_Click(object sender, RoutedEventArgs e)
        {
            BtnOnlineMode.Style = (Style)Styles[styleInactiveMode];
            BtnOfflineMode.Style = (Style)Styles[styleInactiveMode];
            BtnManualMode.Style = (Style)Styles[styleActiveMode];

            OnlineActive = false;
            OfflineActive = false;
            ManualActive = true;
        }
        #endregion

        #region LKDSFramework Events
        private void Driver_OnReceiveData(PackV7 pack)
        {
            if (FocusedDev != null)
            {
                if (pack.CanID != FocusedDev.CanID)
                {
                    return;
                }
            }

            if (pack is LKDSFramework.Packs.DataDirect.PackV7IAPService)
            {
                LKDSFramework.Packs.DataDirect.PackV7IAPService IAPAns = (LKDSFramework.Packs.DataDirect.PackV7IAPService)pack;
                if (IAPAns.Error != LKDSFramework.Packs.DataDirect.PackV7IAPService.IAPErrorType.NoError)
                {
                    //WriteError(IAPAns);
                }
                else
                {
                    if (IAPAns is LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPStateAns)
                    {
                        LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPStateAns PackStateAns = IAPAns as LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPStateAns;
                        FileExt = "*.b" + PackStateAns.IAPState.AppVer.ToString("X2");
                        /*Invoke(new Action(() =>
                        {
                            SelectFirmwareBTN.Enabled = true;
                            LBAndSubDeviceLV.Enabled = true;
                        }));*/
                        //FwOnPages.Clear();
                        /*FWView(PackStateAns);
                        WriteRecieve(IAPAns);*/
                        //GenerateBTN(IAPAns);
                    }
                    else if (!SendLastFragFlag && IAPAns is LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPWriteAns)
                    {
                        FirmwareLoadPackAns = pack as LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPWriteAns;

                        Console.WriteLine(FirmwareLoadPackAns.Offset);
                        //WriteRecieve(FirmwareLoadPackAns);

                        int Pos = FirmwareLoadPackAns.Offset * 32 / PartSize;
                        int Offset = FirmwareLoadPackAns.Offset;
                        SendLastFragFlag = Pos == FirmwareFragments.Count - 1;

                        if (Pos > FirmwareFragments.Count)
                        {
                            throw new Exception("нет такого");
                        }

                        Driver.SendPack(new LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPWriteAsk()
                        {
                            UnitID = FocusedDev.UnitID,
                            CanID = FocusedDev.CanID,
                            Num = (byte)PageNum,
                            Fragment = new LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPWriteAsk.FragmentPageSturct()
                            {
                                Buff = FirmwareFragments[Pos],
                                Offset = (short)Offset,
                                isLastFrag = SendLastFragFlag
                            }
                        });

                        //WriteRecieve(IAPAns);
                    }
                    else if (IAPAns is LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPUpdateAns)
                    {
                        //WriteRecieve(IAPAns);
                    }
                    else if (IAPAns is LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPClearAns)
                    {
                        //WriteRecieve(IAPAns);
                    }
                    else if (IAPAns is LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPReadAns)
                    {
                        LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPReadAns packV7IAPReadAns = IAPAns as LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPReadAns;
                        FwOnPages.Add(packV7IAPReadAns.PageState.Name);

                        
                        VMDevice VMDev = null;
                        /*FWGet = false;
                        if (FWGet)
                        {

                        }*/
                        Dispatcher.Invoke((Action)(() =>
                        {
                            stopwatch.Start();
                            progressValue++;
                            string FWVer = "";
                            char[] FWName = packV7IAPReadAns.PageState.Name.ToCharArray();
                            for (int i = FWName.Length - 1; i > 0; i--)
                            {
                                if (Char.IsDigit(FWName[i]) || FWName[i].Equals('0'))
                                {
                                    FWVer += FWName[i];
                                }
                                else
                                {
                                    try
                                    {
                                        int a = (int)FWName[i];
                                        if (a.Equals(32))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    catch
                                    {
                                        break;
                                    }
                                }
                            }
                            FWName = FWVer.Reverse().ToArray();
                            FWVer = "";
                            
                            foreach (char ch in FWName)
                            {
                                FWVer += ch + ".";
                            }
                            FWVer = FWVer.Trim();
                            SubDeviceV7 dev;
                            if (pack.CanID.Equals(0) && LBCheck)
                            {
                                return;
                            }
                            else
                            {
                                dev = Driver.Devices[0];
                            
                                LBCheck = true;
                            }
                            if (!pack.CanID.Equals(0))
                            {
                                dev = Driver.Devices[0].SubDevices[pack.CanID];
                            }
                            
                            VMDev = new VMDevice()
                            {
                                CanID = dev.CanID,
                                Title = dev.ToString(),
                                AppVer = dev.AppVer,
                                FWTitle = packV7IAPReadAns.PageState.Name,
                                FWVersion = FWVer,
                                FWDate = "Не определено"
                            };
                            
                            stopwatch.Stop();
                            TimeSpan Ts = stopwatch.Elapsed;
                            time = CounterDevSubdev - progressValue * (long)(Ts.Milliseconds) / 1000;
                            
                            long min = time / 60;
                            long sec = time - min * 60;
                            OcVMDev.Add(VMDev);
                            LbRemainingTime.Content = $"{min}мин{sec}сек";
                            LbDevCount.Content = CounterDevSubdev.ToString();
                            LVCanDevList.Items.Refresh();
                            PbMain.Value = progressValue;
                        }));



                    }
                    else
                    {
                        //WriteRecieve(IAPAns);
                    }
                }
            }
        }

        private void Driver_OnSubDevChange(SubDeviceV7 dev)
        {

            try
            {

                Dispatcher.Invoke((Action)(() =>
                {
                    FWGet = true;
                    LVCanDevList.Items.Refresh();
                    VMDevice.SendReadAsk(dev.CanID);

                    CounterDevSubdev++;

                    PbMain.Maximum = CounterDevSubdev;


                    CheckAnalyze = true;
                }));


                //
                // Update device
                //
                if (SubDevices.Count > 0)
                {
                    foreach (SubDeviceV7 addedDev in SubDevices)
                    {
                        if (addedDev.CanID.Equals(dev.CanID))
                        {
                            SubDevices.Remove(addedDev);
                            SubDevices.Add(dev);
                            return;
                        }
                    }
                }

                SubDevices.Add(dev);
            }
            catch { }
        }

        #endregion

        #region Other Events
        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (OnlineActive)
            {
                // Скачать файл с прошивками
                // Распарсить
                //
                //
                // Запустить цикл в котором будет поочередное обновление каждого устройства
                // Итерация не закончится, пока не обновится прошивка на устройстве
                // Нужен список с id устройств (мб коллекцию юзать)
            } else if (OfflineActive)
            {
                // Нужно сверстать окно, подготовка к диплому
                //
                // 
                /*WndOfflineMode wndOfflineMode = new WndOfflineMode();
                wndOfflineMode.ShowDialog();*/
            }
            else if (ManualActive)
            {
                WndManualMode wndManualMode = new WndManualMode();
                wndManualMode.ShowDialog();
                // Доверстать окно
                //
            }
        }

        private void LVCanDevList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!LVCanDevList.SelectedItem.Equals(null))
            {
                BtnUpdate.IsEnabled = true;
                BtnUpdate.Style = (Style)Styles[styleUpdateEnable];
            }
            else
            {
                BtnUpdate.IsEnabled = false;
                BtnUpdate.Style = (Style)Styles[styleUpdateDisable];
            }
        }

        #endregion

        //
        // Мб навести порядок в коде
        // 

        /*void WriteRecieve(LKDSFramework.Packs.DataDirect.PackV7IAPService IAPAns)
        {
            try
            {
                this.Invoke(new Action(() =>
                {
                    SendRecievePacksRTB.Text += $"Recieve Pack '{IAPAns.IAPCommand}', ";
                    SendRecievePacksRTB.Text += $"on Device {FocusedDev.CanID}, {ActivePageTxt}";
                    SendRecievePacksRTB.Text += $"\n-----------------Result: -----------------" +
                   $"\nPack id: {IAPAns.PackID}" +
                   $"\nPack class: {IAPAns.Class}" +
                   $"\nPack type: {IAPAns.Type}" +
                   $"\nPack state: {IAPAns.State}" +
                   $"\nPack data: ";
                    foreach (byte bt in IAPAns.Data)
                    {
                        SendRecievePacksRTB.Text += $"{bt} ";
                    }
                    SendRecievePacksRTB.Text += $"\n------------------------------------------\n\n";
                    SendRecievePacksRTB.SelectionStart = SendRecievePacksRTB.TextLength;
                    SendRecievePacksRTB.ScrollToCaret();


                }));
            }
            catch { };



        }


        void FWView(LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPStateAns pack)
        {

            for (int i = 1; i != pack.IAPState.PageCount; i++)

                Driver.SendPack(new LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPReadAsk()
                {
                    UnitID = FocusedDev.UnitID,
                    Num = (byte)i,
                    CanID = FocusedDev.CanID
                });
        }


        void SendStateAsk()
        {
            Driver.SendPack(new LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPStateAsk()
            {
                UnitID = FocusedDev.UnitID,
                CanID = FocusedDev.CanID
            });
        }

        void SendReadAsk()
        {
            Driver.SendPack(new LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPReadAsk()
            {
                UnitID = FocusedDev.UnitID,
                CanID = FocusedDev.CanID,
                Num = (byte)PageNum
            });
        }

        void SendUpdateAsk()
        {
            Driver.SendPack(new LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPUpdateAsk()
            {
                UnitID = FocusedDev.UnitID,
                CanID = FocusedDev.CanID,
                Num = (byte)PageNum
            });
        }

        void SendActivateAsk()
        {
            Driver.SendPack(new LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPActiveAsk()
            {
                UnitID = FocusedDev.UnitID,
                CanID = FocusedDev.CanID,
                Num = (byte)PageNum
            });
        }

        void SendClearAsk()
        {
            Driver.SendPack(new LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPClearAsk()
            {
                UnitID = FocusedDev.UnitID,
                CanID = FocusedDev.CanID,
                Num = (byte)PageNum
            });
        }*/
    }
}
