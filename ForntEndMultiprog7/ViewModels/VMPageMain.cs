﻿using ForntEndMultiprog7.Model;
using LKDSFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Data;
using System.Windows;
using System.Diagnostics;
using FrontEndMultiprog7.Windows;

namespace ForntEndMultiprog7.ViewModels
{




    // online
    // offline
    // manualmode
    //
    // Выбор 1 из трех режимов
    // Разное по событию клик на любой из них
    // Мануал - открыть окно и выбрать прошивку
    // Онлайн - обновить каждое устройство из списка Firmware.xml
    // Офлайн - обновить каждое устройство из подготовленного списка (нетнетнет)
    public class VMPageMain : BaseViewModel
    {
        #region Vars
        //
        // int type
        //
        const int PartSize = 928;
        int PageNum = 1;

        //
        // Lkds types
        //
        public static DriverV7 Driver = new DriverV7();
        LKDSFramework.Packs.DataDirect.IAPService.PackV7IAPWriteAns FirmwareLoadPackAns;
        SubDeviceV7 FocusedDev = null;

        //
        // lists
        //
        List<SubDeviceV7> SubDevices = new List<SubDeviceV7>();
        List<byte[]> FirmwareFragments = new List<byte[]>();
        List<string> FwOnPages = new List<string>();
        //
        // string type
        //
        public static string FileExt;
        public static string FileFW;

        //
        // byte type
        //
        byte SelectedDevOrSubDevCANID;

        //
        // bool type
        //
        bool FWGet = false;
        bool LBCheck = false;
        bool CheckAnalyze = false;
        bool SendLastFragFlag = false;
        bool OnlineActive = true;
        bool OfflineActive = false;
        bool ManualActive = false;
        //
        // long type
        //
        private long time = 0;

        //
        // object
        //
        private object _lock = new object();

        //
        // other types
        //
        Stopwatch stopwatch = new Stopwatch();

        #endregion

        #region Var to view
        //
        // string type
        //
        private string timeview = "";
        private string labelContentUpdate = "Обновление";
        private string labelContentAnalyze = "Анализ";

        private string styleInactiveMode = "{StaticResource BtnActiveMode}";
        private string styleActiveMode = "{StaticResource BtnInactiveMode}";
        private string styleUpdateDisable = "{StaticResource BtnUpdateEnabled}";
        private string styleUpdateEnable = "{StaticResource BtnUpdateDisabled}";
        private string selectedStyle;
        //
        // bool type
        //
        //
        private bool isEnableUpdate = false;
        // int type
        //
        private int progressMax = 1;
        private int progressValue = 0;
        private int counterDevSubdev = 0;

        //
        // other type
        //
        private ObservableCollection<VMDevice> ocVMDevice;
        private int selectedIndex;

        static ResourceDictionary myResourceDictionary = new ResourceDictionary
        {
            Source = new Uri(@"\ResourceDictionaries\Styles.xaml", UriKind.RelativeOrAbsolute)
        };




        #endregion

        #region Properties to view

        // 
        // int type
        //
        public int ProgressMax { get { return progressMax; } }
        public int ProgressValue { get { return progressValue; } }
        public int CounterDevSubdev { get { return counterDevSubdev; } }

        //
        // string type
        //
        public string SelectedStyle
        {
            get { return this.selectedStyle; }
            set
            {
                this.selectedStyle = value;
            }
        }
        public string LabelContentView
        {
            get
            {
                if (CheckAnalyze)
                {
                    return labelContentAnalyze;
                }
                else
                {
                    return labelContentUpdate;
                }
            }
        }

        public string StyleChangeOnline
        {
            get
            {
                if (OnlineActive)
                {
                    return styleActiveMode;
                }
                else
                {
                    return styleInactiveMode;
                }
            }
        }

        public string StyleChangeOffline
        {
            get
            {
                if (OfflineActive)
                {
                    return styleActiveMode;
                }
                else
                {
                    return styleInactiveMode;
                }
            }
        }

        public string StyleChangeManual
        {
            get
            {
                if (ManualActive)
                {
                    return styleActiveMode;
                }
                else
                {
                    return styleInactiveMode;
                }
            }
        }

        public string StyleChangeUpdate
        {
            get
            {
                if (isEnableUpdate)
                {
                    return styleUpdateEnable;
                }
                else
                {
                    return styleUpdateDisable;
                }
            }
        }
        public string TimeView { get { return timeview; } }

        //
        // int type
        //
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                selectedIndex = value;
                OnPropertyChanged();
                isEnableUpdate = true;
                OnPropertyChanged(nameof(IsEnableUpdate));
                OnPropertyChanged(nameof(StyleChangeUpdate));
            }
        }
        //
        // bool type
        //
        public bool IsEnableUpdate { get { return isEnableUpdate; } }
        //
        // other type
        //
        //public ObservableCollection<VMDevice> OcVMDevice { get { return ocVMDevice; } }
       

        #endregion

        #region Cmds

        #region vars cmd

        private RelayCommand cmdOnline;
        private RelayCommand cmdOffline;
        private RelayCommand cmdManualMode;
        private RelayCommand cmdUpdate;

        #endregion

        #region properties cmd
        public RelayCommand Online_Click
        {
            get
            {
                return cmdOnline ??
                  (cmdOnline = new RelayCommand(obj =>
                  {
                      

                  }));
            }
        }

        public RelayCommand Manual_Click
        {
            get
            {
                return cmdManualMode ??
                  (cmdManualMode = new RelayCommand(obj =>
                  {
                      WndManualMode wnd = new WndManualMode();
                      wnd.ShowDialog();
                  }));
            }
        }

        #endregion

        #endregion

        public VMPageMain()
        {
            ocVMDevice = new ObservableCollection<VMDevice>();
            BindingOperations.EnableCollectionSynchronization(ocVMDevice,_lock);


            Driver.OnSubDevChange += Driver_OnSubDevChange; ;
            Driver.OnReceiveData += Driver_OnReceiveData; ;
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

                    /*LVCanDevList.ItemsSource = OcVMDevice;
                    lbConnectionForm.Device = Devices[0];
                    IsConnected = true;
                    lbConnectionForm.Enabled = false;*/
                }
                catch { }
            }

        }

        private void Driver_OnReceiveData(LKDSFramework.Packs.PackV7 pack)
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
                        }));
                        FwOnPages.Clear();
                        FWView(PackStateAns);
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
                        Console.WriteLine(packV7IAPReadAns.PageState.Name);
                        Console.WriteLine(packV7IAPReadAns.PageState.App);
                        Console.WriteLine(packV7IAPReadAns.PageState.Description);
                        Console.WriteLine(packV7IAPReadAns.PageState.Lenght);
                        Console.WriteLine(packV7IAPReadAns.PageState.Soft);
                        Console.WriteLine(packV7IAPReadAns.PageState.UnitType);
                        VMDevice VMDev = null;

                            /*FWGet = false;
                            if (FWGet)
                            {
                                
                            }*/
                        lock (_lock)
                        {
                            stopwatch.Start();
                            progressValue++;

                            string FWVer = "";
                            char[] FWName = packV7IAPReadAns.PageState.Name.ToCharArray();
                            for (int i = FWName.Length-1; i>0 ; i--)
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
                                        } else
                                        {
                                            break;
                                        }
                                    } catch
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
                            FillOc(VMDev);
                            stopwatch.Stop();
                            TimeSpan Ts = stopwatch.Elapsed;
                            time = counterDevSubdev-progressValue * (long)(Ts.Milliseconds) / 1000;

                            long min = time / 60;
                            long sec = time - min * 60;
                            timeview = $"{min}мин{sec}сек";
                            OnPropertyChanged(nameof(TimeView));
                            OnPropertyChanged(nameof(ProgressValue));
                        }
                        //WriteRecieve(IAPAns);
                    }
                }
                   
            }
            else
            {
                //WriteRecieve(IAPAns);
            }
        }

        private void Driver_OnSubDevChange(SubDeviceV7 dev)
        {
            try
            { 
                lock (_lock)
                {
                    FWGet = true;
                    VMDevice.SendReadAsk(dev.CanID);

                    progressMax = counterDevSubdev;
                    OnPropertyChanged(nameof(ProgressMax));

                    counterDevSubdev++;
                    OnPropertyChanged(nameof(CounterDevSubdev));
                    
                    CheckAnalyze = true;
                    OnPropertyChanged(nameof(LabelContentView));
                }
               

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


        public void FillOc(VMDevice vMDevice)
        {
           /* OcVMDevice.Add(vMDevice);
            OnPropertyChanged(nameof(OcVMDevice));*/
        }
    }


}
