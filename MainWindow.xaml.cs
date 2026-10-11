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
using NAudio.Wave;
using NAudio.Wave.Asio;
using Microsoft.Win32; 

namespace AxeControlPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string targetDriver = "AXE IO ONE";
        private AsioDriver? persistentDriver = null;

        // Custom Terminal Parameters
        private bool isTerminalExpanded = false;


        public MainWindow()
        {
            InitializeComponent();
            ComboSampleRate.SelectedIndex = 0;
            CheckAsioDriverStatus();

            // Temporary
            LogToTerminal("warning", "Currently in development..");
        }

        #region Apply Button
        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBuffer.SelectedItem == null)
            {
                MessageBox.Show("Selecteer eerst een buffer size in de dropdown!",
                                "Axe Control Panel",
                                 MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            int desiredBuffer = (int)ComboBuffer.SelectedItem;

            try
            {
                AsioDriver driver = AsioDriver.GetAsioDriverByName(targetDriver);
                if (driver != null)
                {
                    IntPtr windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    driver.Init(windowHandle);

                    if (ComboSampleRate.SelectedItem is ComboBoxItem selectedItem)
                    {
                        string? selectedSampleStr = ((ComboBoxItem)ComboSampleRate.SelectedItem).Content.ToString();

                        if (!string.IsNullOrEmpty(selectedSampleStr))
                        {
                            double selectedSampleRate = double.Parse(selectedSampleStr);
                            try
                            {
                                driver.SetSampleRate(selectedSampleRate);
                                TxtLiveSampleRate.Text = $"RATE > {selectedSampleRate}";
                                TxtLiveSampleRate.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 100));
                            }
                            catch
                            {

                            }
                        }

                        MessageBox.Show($"Buffer instelling verzonden naar {desiredBuffer}");
                        if (ChkKeepAlive.IsChecked != true)
                        {
                            driver.ReleaseComAsioDriver();
                            TxtCurrentBuffer.Text = $"ASIO MODULE > CLOCK ENGAGED // DRIVER RELEASED";
                        }
                        else
                        {
                            TxtCurrentBuffer.Text = $"ASIO MODULE > CLOCK LOCKED ON {desiredBuffer} SAMPLES // KEEP ALIVE ACTIVE";
                        }
                    }

                    if (ChkRegistryInject.IsChecked == true)
                    {
                        ScanAndWriteRegistry(desiredBuffer);
                    }
                    else
                    {
                        TxtCurrentBuffer.Text = $"ASIO MODULE > CLOCK FORCED TO {desiredBuffer} SAMPLES";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kon de buffer niet forceren: {ex.Message}\n\nZorg ervoor dat overige geluidsprogramma's volledig zijn afgesloten!",
                                "Hardware Component Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Keep Alive Button
        private void ChkKeepAlive_Changed(object sender, EventArgs e) {
            if (TxtStatus == null || TxtLiveSampleRate == null || ComboSampleRate == null) return;

            try
            {
                string currentSampleRate = "";
                if(ComboSampleRate.SelectedItem is ComboBoxItem selectedItem)
                {
                    currentSampleRate = selectedItem.Content?.ToString() ?? "44100";
                }

                if (ChkKeepAlive.IsChecked == true)
                {
                    if (persistentDriver == null)
                    {
                        persistentDriver = AsioDriver.GetAsioDriverByName(targetDriver);
                        if (persistentDriver != null)
                        {
                            IntPtr windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                            persistentDriver.Init(windowHandle);

                            TxtStatus.Text = "CORE STATUS > KEEP ALIVE ACTIVE (DRIVER LOCKED)";
                            TxtStatus.Foreground = System.Windows.Media.Brushes.Cyan;
                        }
                    }
                }
                else
                {
                    if (persistentDriver != null)
                    {
                        try
                        {
                            persistentDriver.ReleaseComAsioDriver();
                            System.Threading.Thread.Sleep(50);
                        }
                        catch { }

                        System.Runtime.InteropServices.Marshal.ReleaseComObject(persistentDriver);
                        persistentDriver = null;

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    TxtStatus.Text = "CORE STATUS > LIVE CONNECTION TO ASIO CORE";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Green;

                    TxtLiveSampleRate.Text = $"CLOCK RATE > {currentSampleRate} HZ";
                    TxtLiveSampleRate.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 100));

                    CheckAsioDriverStatus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Keep Alive Fout: {ex.Message}", "Driver Lock Error");
                ChkKeepAlive.IsChecked = false;
            }
        }
        #endregion

        #region Core Asio Connection
        private void CheckAsioDriverStatus()
        {
            try
            {
                string[] drivers = AsioOut.GetDriverNames();
                if (!Array.Exists(drivers, name => name == targetDriver))
                {
                    TxtStatus.Text = $"Status: {targetDriver} niet gevonden!";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                AsioDriver driver = AsioDriver.GetAsioDriverByName(targetDriver);
                if (driver != null)
                {
                    IntPtr windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    driver.Init(windowHandle);

                    try
                    {
                        driver.SetSampleRate(44100.0);
                    }
                    catch { }


                    TxtStatus.Text = $"Status: Live verbonden met de ASIO Core ({driver.GetDriverName()})!";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Green;

                    driver.GetBufferSize(out int minSize, out int maxSize, out int preferedSize, out int granularity);
                    double actualRate = driver.GetSampleRate();
                    
                    TxtCurrentBuffer.Text = $"Current active buffer: {preferedSize} samples";
                    TxtLiveSampleRate.Text = $"CLOCK RATE > HARDWARE LOCKED: {actualRate}HZ";

                    ComboBuffer.Items.Clear();
                    int[] standardSizes = { 64, 128, 256, 512, 1024, 2048 };

                    if (granularity <= 0)
                    {
                        for (int size = minSize; size <= maxSize; size *= 2)
                        {
                            ComboBuffer.Items.Add(size);
                        }
                    }
                    else
                    {
                        for (int size = minSize; size <= maxSize; size += granularity)
                        {
                            ComboBuffer.Items.Add(size);
                        }
                    }

                    ComboBuffer.SelectedItem = preferedSize;
                    driver.ReleaseComAsioDriver();
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Status: Driver bezet of geblokkeerd.";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                TxtCurrentBuffer.Text = $"Fout: {ex.Message}";
            }
        }
        #endregion

        #region Registry Scan
        private void ScanAndWriteRegistry(int desiredBuffer)
        {
            // Key Path regarding 'IK Multimedia'
            string ikKeyPath = @"Software\IK Multimedia\AXE IO ONE";

            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(ikKeyPath, true))
                {
                    if (key != null)
                    {
                        string[] valueNames = key.GetValueNames();
                        string bufferValueName = "";

                        foreach (string name in valueNames)
                        {
                            if (name.Contains("Buffer", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Size", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Block", StringComparison.OrdinalIgnoreCase))
                            {
                                bufferValueName = name;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(bufferValueName))
                        {
                            key.SetValue(bufferValueName, desiredBuffer, RegistryValueKind.DWord);
                            TxtCurrentBuffer.Text = $"Buffer geforceerd in Register: {desiredBuffer} samples";
                        }
                        else
                        {
                            TxtCurrentBuffer.Text = "Sleuter gevonden, maar 'Buffer' DWORD ontbreekt.";
                        }
                    }
                    else
                    {
                        ScanAlternativeIKRegistry();
                    }
                }
            }
            catch (Exception ex)
            {
                TxtCurrentBuffer.Text = $"Registry Fout: {ex.Message}";
            }
        }
        #endregion

        #region Alternate(Deep) Registry Scan
        private void ScanAlternativeIKRegistry()
        {
            try
            {
                using (RegistryKey? baseKey = Registry.CurrentUser.OpenSubKey(@"Software\IK Multimedia", true))
                {
                    if (baseKey != null)
                    {
                        string[] subKeys = baseKey.GetSubKeyNames();
                        string foundSubKeys = string.Join(", ", subKeys);

                        TxtCurrentBuffer.Text = $"IK Root found. Subkeys: {foundSubKeys}";
                    }
                    else
                    {
                        TxtCurrentBuffer.Text = "IK Multimedia root key missing in CurrentUser.";
                    }
                }
            }
            catch (Exception ex)
            {
                TxtCurrentBuffer.Text = $"Alternative scan failed: {ex.Message}";
            }
        }
        #endregion

        private void ComboSampleRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { 
            if (TxtLiveSampleRate == null) return;

            if(ComboSampleRate.SelectedItem is ComboBoxItem item)
            {
                TxtLiveSampleRate.Text = $"CLOCK RATE > DESIRED: {item.Content} HZ";
                TxtLiveSampleRate.Foreground = System.Windows.Media.Brushes.Gold;
            }
        }

        private void BtnToggleTerminal_Click(object sender, RoutedEventArgs e)
        {
            double baseTerminalWidth = 535;

            if (isTerminalExpanded)
            {
                LogToTerminal("system", "Log window closed. Actively monitoring in the background.");

                this.Width = baseTerminalWidth;
                BtnToggleTerminal.Content = ">";
                isTerminalExpanded = false;
            }
            else
            {
                this.Width = 1050;
                BtnToggleTerminal.Content = "<";
                isTerminalExpanded = true;

                LogToTerminal("system", "Log window opened. Starting live background monitoring...");
                LogToTerminal("success", "Succesfully connected to terminal.");
            }
        }

        private void LogToTerminal (string prefix, string message)
        {
            if(TxtTerminal == null || TerminalScroll == null) return;

            string timeStamp = DateTime.Now.ToString("HH:mm:ss");

            // 1.
            System.Windows.Media.Brush prefixColor;
            switch(prefix.ToLower())
            {
                case "system":
                    prefixColor = System.Windows.Media.Brushes.Silver;
                    break;
                case "warnig":
                    prefixColor = System.Windows.Media.Brushes.Orange;
                    break;
                case "error":
                    prefixColor = System.Windows.Media.Brushes.Crimson;
                    break;
                case "success":
                    prefixColor = System.Windows.Media.Brushes.LimeGreen;
                    break;
                case "lock":
                    prefixColor = System.Windows.Media.Brushes.Cyan;
                    break;
                case "registry":
                    prefixColor = System.Windows.Media.Brushes.Gold;
                    break;
                case "asio":
                    prefixColor = System.Windows.Media.Brushes.DeepSkyBlue;
                    break;
                default:
                    prefixColor = System.Windows.Media.Brushes.DarkGray;
                    break;

            }

            // 2.
            Run timeRun = new Run($"[{timeStamp}] ") { Foreground = System.Windows.Media.Brushes.DimGray };
            Run prefixRun = new Run($"[{prefix.ToUpper()}] ") { Foreground = prefixColor, FontWeight = System.Windows.FontWeights.Bold };
            Run messageRun = new Run($"{message}\n") { Foreground = System.Windows.Media.Brushes.LightGray };

            // 3.
            TxtTerminal.Inlines.Add(timeRun);
            TxtTerminal.Inlines.Add(prefixRun);
            TxtTerminal.Inlines.Add(messageRun);

            // 4.
            TerminalScroll.ScrollToBottom();
        }
    }
}