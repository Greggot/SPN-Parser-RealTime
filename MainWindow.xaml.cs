using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SPNParser
{
    public class SPNblock   // DockPanel with Input and Output TextBlock
    {
        bool stateNormal = true;    // true SPN -> Float, Float -> SPN
        string InputString = "";    // HEX value from CAN
        string OutputString = "";   // Float value counted via SPN
        private string Measure = "";    // String added to Output String
        public DockPanel Panel = new DockPanel();
        public TextBlock Input = new TextBlock();
        public TextBlock Output = new TextBlock();
        string[] splitData;     // Operation sequence from script file

        private void Recount()      // SPN -> Float
        {
            float value = 0;
            if(InputString.Length > 0)
                value = Convert.ToSingle(Int64.Parse(InputString, System.Globalization.NumberStyles.HexNumber));
            
            for (int i = 0; i < splitData.Length; i++)
            {
                switch(splitData[i])
                {
                    case "+":
                        value += Convert.ToSingle(splitData[i + 1]);
                        break;
                    case "-":
                        value -= Convert.ToSingle(splitData[i + 1]);
                        break;
                    case "/":
                        value /= Convert.ToSingle(splitData[i + 1]);
                        break;
                    case "*":
                        value *= Convert.ToSingle(splitData[i + 1]);
                        break;
                }
            }

            OutputString = value.ToString();
        }

        private void ReverseRecount()   // Float -> SPN
        {
            float value = 0x0;
            if(OutputString.Length > 0)
                value = Convert.ToSingle(OutputString);
            for (int i = splitData.Length - 1; i > 0; i--)
            {
                switch(splitData[i - 1])
                {
                    case "+":
                        value -= Convert.ToSingle(splitData[i]);
                        break;
                    case "-":
                        value += Convert.ToSingle(splitData[i]);
                        break;
                    case "/":
                        value *= Convert.ToSingle(splitData[i]);
                        break;
                    case "*":
                        value /= Convert.ToSingle(splitData[i]);
                        break;
                }
            }

            InputString = Convert.ToString(Convert.ToInt64(value), 16);
            InputString = InputString.ToUpper();
        }
        private void Refresh()      // Recount values and update TextBoxes
        {
            if(stateNormal)
                Recount();
            else
                ReverseRecount();

            Input.Text = InputString;
            Output.Text = OutputString + " " + Measure;
        }
        private void KeyboardInput(object sender, KeyEventArgs e)      // TextBox keyboard input
        {
            if(e.Key == Key.R)      // Switch commutation states
                stateNormal = !stateNormal;
            if(stateNormal)
            {
                switch(e.Key)
                {
                    case Key.Return:        // Clear 
                        InputString = "";
                        Refresh();
                        break;
                    case Key.Back:
                        if(InputString.Length > 0)
                            InputString = InputString.Remove(InputString.Length - 1);
                        Refresh();
                        break;
                    default:
                        if(InputString.Length < 16)
                        {
                            if(e.Key >= Key.D0 && e.Key <= Key.D9)
                            {
                                InputString += (e.Key - Key.D0).ToString();
                                Refresh();
                            }
                            else if(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                            {
                                InputString += (e.Key - Key.NumPad0).ToString();
                                Refresh();
                            }
                            else if(e.Key >= Key.A && e.Key <= Key.F)
                            {
                                InputString += e.Key.ToString();
                                Refresh();
                            }
                        }
                        break;
                }
            }
            else
            {
                switch(e.Key)
                {
                    case Key.Back:
                        if(InputString.Length > 0)
                            OutputString = OutputString.Remove(OutputString.Length - 1);
                        Refresh();
                        break;
                    case Key.OemComma:
                        if(OutputString.IndexOf(",") < 0)
                        {
                            OutputString += ",";
                            Refresh();
                        }
                        break;
                    case Key.Decimal:
                        if(OutputString.IndexOf(",") < 0)
                        {
                            OutputString += ",";
                            Refresh();
                        }
                        break;
                    
                    default:
                        if(e.Key >= Key.D0 && e.Key <= Key.D9)
                        {
                            OutputString += (e.Key - Key.D0).ToString();
                            Refresh();
                        }
                        else if(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                        {
                            OutputString += (e.Key - Key.NumPad0).ToString();
                            Refresh();
                        }
                        break;
                }
            }
        }
        private void MouseFocus(object caller, MouseButtonEventArgs e)
        {
            Panel.Focus();
        }
        public SPNblock(string Measure, string path)
        {
            if(File.Exists(path))       // If there's path to a script file
            {
                string[] Data = File.ReadAllLines(path);
                splitData = Data[0].Split(' ', Data[0].Length);
            }

            this.Measure = Measure;

            Panel = new DockPanel();
            Panel.Focusable = true;
            Keyboard.AddKeyDownHandler(Panel, KeyboardInput);
            Mouse.AddPreviewMouseDownHandler(Panel, MouseFocus);

            Input.Text = "0";
            Input.Width = 200;

            Output.Text = "Output" + " " + Measure;
            Output.Width = 200;

            DockPanel.SetDock(Input, Dock.Left);

            Panel.Children.Add(Input);
            Panel.Children.Add(Output);
        }
    }


    public partial class MainWindow : Window    // Главное окно
    {
        const int CanvasDistance = 30;    // Рассмотяние между блоками SPN по Y
        int LastTop = CanvasDistance;   // Координата последнего блока по Y
        void AddSPNblock(string Measure, string script)     // Добавление блока SPN
        {
            SPNblock SPN = new SPNblock(Measure, script);  

            Canvas.SetTop(SPN.Panel, LastTop);      // Отодвинуть блок по Y
            LastTop += CanvasDistance;              // Изменить координату последнего блока
            Canvas.SetLeft(SPN.Panel, 20);          // Отодвинуть блок по X на коснтанту, чтобы блоки были в одном столбце

            BackCanvas.Children.Add(SPN.Panel);    // Добавить блоки в канвас

            SPN.Panel.Focus();          // Сфокусироваться на последнем блоке
        }

        private void RootKeyInput(object sender, KeyEventArgs e)    // Бинд клавиш главного окна
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control     // Ctrl + N
                && e.Key == Key.N)
            {
                MessageBox.Show("There\'l be Creation Soon");      
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control 
                && e.Key == Key.D || e.Key == Key.Delete)       // Ctrl + D
            {
                MessageBox.Show("There\'l be Deletion Soon");
            }
            else if(e.Key == Key.Escape)        // На Esc прога тупа закрываеца)))
                this.Close();
        }
        public MainWindow()
        {
            InitializeComponent();

            Root.Focusable = true;      // Сделать так, что окно теперь может обрабатывать нажатие клавиш
            Keyboard.AddKeyDownHandler(Root, RootKeyInput);     // Передать функцию-обработчик 

            if(!Directory.Exists("scripts"))           // Если нет папки со скриптами
                Directory.CreateDirectory("scripts");   // Создать такую

            AddSPNblock("K", "scripts/scr.txt");        // Добавить блок с единицами в "K", которая считается согласно файлу scr.txt
            AddSPNblock("mph", "scripts/speed.txt");    
            AddSPNblock("C", "scripts/cel.txt");
        }
    }
}