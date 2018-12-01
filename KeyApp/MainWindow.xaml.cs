using KeyApp.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
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

namespace KeyApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variable Declaration
        static private string logFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\meterreader.log";
        static private UberDLMX.DLMX dLMX = new UberDLMX.DLMX();
        static private System.Threading.Thread syncthread = new System.Threading.Thread(sync);
        static private int _sleepTime = Convert.ToInt16(System.Configuration.ConfigurationManager.AppSettings["SleepKey"]);
        static private int syncInterval = Convert.ToInt32(TimeSpan.FromMinutes(_sleepTime).TotalMilliseconds);
        static private string _apiKey = null;
        static private string _societyIDKey = null;
        static private string _txtAPIKey = null;
        static private string _txtSocietyID = null;
        #endregion

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                txtSocietyID.Focus();
            }
            catch (Exception _exception)
            {
                MessageBox.Show(_exception.Message.ToString(), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #region UserDefinedMethods

        static private void sync()
        {
            var readDataClient = new ReadingServiceReference.UberServiceClient("BasicHttpBinding_IUberService");
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            do
            {
                try
                {
                    if (config.AppSettings.Settings.Count > 1)
                    {
                        _apiKey = config.AppSettings.Settings["APIKey"].Value;
                        _societyIDKey = config.AppSettings.Settings["SocietyKey"].Value;
                    }
                    else
                    {
                        config.AppSettings.Settings.Add("APIKey", _txtAPIKey);
                        config.AppSettings.Settings.Add("SocietyKey", _txtSocietyID);
                        config.Save(ConfigurationSaveMode.Modified);
                        _apiKey = config.AppSettings.Settings["APIKey"].Value; //System.Configuration.ConfigurationManager.AppSettings["APIKey"];
                        _societyIDKey = config.AppSettings.Settings["SocietyKey"].Value; //System.Configuration.ConfigurationManager.AppSettings["SocietyKey"];
                    }

                    if (_apiKey != null && _apiKey.Length > 0 && _societyIDKey != null && _societyIDKey.Length > 0)
                    {
                        if (readDataClient == null)
                        {
                            readDataClient = new ReadingServiceReference.UberServiceClient("BasicHttpBinding_IUberService");
                            WriteToLog(DateTime.Now + "New Object Initialized For GetHouseDetails");
                        }
                        if (readDataClient != null)
                        {
                            DataTable objHouseDetailsdataTable = new DataTable();
                            if (readDataClient == null)
                            {
                                readDataClient = new ReadingServiceReference.UberServiceClient("BasicHttpBinding_IUberService");
                                WriteToLog(DateTime.Now + "New Object Initialized For GetHouseDetails");
                            }
                            objHouseDetailsdataTable = readDataClient.GetHouseDetails(_societyIDKey, _apiKey);

                            if (objHouseDetailsdataTable != null && objHouseDetailsdataTable.Rows.Count > 0)
                            {
                                foreach (DataRow datarowItem in objHouseDetailsdataTable.Rows)
                                {
                                    var _soceityID = datarowItem.Field<string>("SiD");
                                    var _houseID = datarowItem.Field<string>("HiD");
                                    var _houseNo = datarowItem.Field<string>("House No");
                                    var _meterID = datarowItem.Field<string>("MiD");
                                    var _meterType = Convert.ToInt16(datarowItem.Field<Int16>("PiD"));
                                    var _meterSettings = datarowItem.Field<string>("metersetting");
                                    var _ipAddress = datarowItem.Field<string>("IPAddress");
                                    var _port = Convert.ToInt32(datarowItem.Field<string>("Port"));
                                    if (_meterType == 3)
                                    {
                                        //Modbus itembus = Newtonsoft.Json.JsonConvert.DeserializeObject<Modbus>(_meterSettings);
                                        //if (itembus != null && itembus.RiD.Length > 0 && itembus.Address.Length > 0)
                                        //{
                                        //    var _regType = Convert.ToInt32(itembus.RiD);
                                        //    var _startAddress = Convert.ToInt32(itembus.Address);
                                        //    var _qty = itembus.Quantity;
                                        //    var _deviceID = itembus.DeviceID;

                                        //    int[] readHoldingRegisters = null; //ModbusReading.ReadingRegister(_ipAddress, _port, _startAddress, _regType, _qty);
                                        //    //ReadModbus(_ipAddress, _port, _startAddress, _regType);

                                        //    if (readHoldingRegisters != null && readHoldingRegisters.Length > 0)
                                        //    {
                                        //        //objdataTableWriteDLMX.Rows.Add(_soceityID, _houseID, _meterID, _ipAddress, _port, readHoldingRegisters);
                                        //    }
                                        //}
                                    }
                                    else
                                    {
                                        DLMX itemdLMX = Newtonsoft.Json.JsonConvert.DeserializeObject<DLMX>(_meterSettings);

                                        if (itemdLMX != null && itemdLMX.Manufacturer.Length > 0 && itemdLMX.Model.Length > 0)
                                        {
                                            var manufacture = itemdLMX.Manufacturer;
                                            var model = itemdLMX.Model;
                                            var importExport = Convert.ToInt16(itemdLMX.ImportExport);
                                            string[] val = dLMX.DLMXRead(_ipAddress, _port, importExport);
                                            if (val != null && val.Length > 0)
                                            {
                                                try
                                                {
                                                    if (readDataClient == null)
                                                    {
                                                        readDataClient = new ReadingServiceReference.UberServiceClient("BasicHttpBinding_IUberService");
                                                        WriteToLog(DateTime.Now + "New Object Initialized");
                                                    }

                                                    readDataClient.WriteDLMXDetails(_soceityID, _houseNo, _meterID, _ipAddress, Convert.ToString(_port), val[0], val[1]);

                                                    if (val[2] == "1" || val[2] == "2")
                                                    {
                                                        readDataClient.WriteErrorLog(_soceityID, _houseID, _meterID, _ipAddress, Convert.ToString(_port), "FAULT", val[2], val[3]);
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    WriteToLog(DateTime.Now + e.InnerException.ToString());
                                                }
                                            }
                                            else
                                            {
                                                readDataClient.WriteDLMXDetails(_soceityID, _houseID, _meterID, _ipAddress, Convert.ToString(_port), null, null);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {

                    }

                }
                #region
                //catch (CommunicationException _communicationException)
                //{
                //    Console.ForegroundColor = ConsoleColor.Red;
                //    Console.WriteLine(_communicationException.InnerException.ToString());
                //    Console.WriteLine("Got {0}", _communicationException.GetType());
                //    WriteToLog(DateTime.Now + _communicationException.InnerException.ToString() + " : Communication Out Exception");
                //    Console.ResetColor();
                //    //readDataClient.Abort();
                //}
                //catch (TimeoutException _timeoutException)
                //{
                //    Console.ForegroundColor = ConsoleColor.Red;
                //    Console.WriteLine(_timeoutException.InnerException.ToString());
                //    Console.WriteLine("Time Out Exception {0}", _timeoutException.GetType());
                //    WriteToLog(DateTime.Now + _timeoutException.InnerException.ToString() + " : Time Out Exception");
                //    Console.ResetColor();
                //    //readDataClient.Abort();
                //}
                #endregion
                catch (Exception _exception)
                {
                    if (_exception.InnerException != null)
                        WriteToLog(_exception.InnerException.ToString());
                    else
                        WriteToLog(_exception.Message.ToString());
                }

                System.Threading.Thread.Sleep(syncInterval);
            }
            while (true);

        }

        static private void WriteToLog(string _text)
        {
            try
            {
                if (_text != null && _text.Length > 0)
                {
                    System.IO.File.AppendAllText(logFile, _text + DateTime.Now + "\r\n");
                }
            }
            catch (Exception _exception)
            {
                MessageBox.Show(_exception.Message.ToString(), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        #endregion

        #region ClickEvent

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtAPIKey != null && txtSocietyID != null && txtAPIKey.Text.Length > 0 && txtSocietyID.Text.Length > 0)
                {
                    _txtAPIKey = txtAPIKey.Text.ToString();
                    _txtSocietyID = txtSocietyID.Text.ToString();
                    syncthread.Start();
                }
            }
            catch (Exception _exception)
            {
                MessageBox.Show(_exception.Message.ToString(), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception)
            {

                throw;
            }
        }

        #endregion

        #region Event
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
