using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.UI.Dialogs {
    [ValueConversion(typeof(ExpressionBuilder), typeof(string))]
    public class ExpressionBuilderConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return ((ExpressionBuilder)value).Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class ExpressionBuilder {
        public string Name { get; set; }
        public string Abbr { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float DefaultValue { get; set; }
        public char Flag { get; set; } = '\0';

        public ExpressionBuilder(UExpressionDescriptor descriptor) {
            Name = descriptor.name;
            Abbr = descriptor.abbr;
            Min = descriptor.min;
            Max = descriptor.max;
            DefaultValue = descriptor.defaultValue;
            Flag = descriptor.flag;
        }

        public ExpressionBuilder() {
            Name = "new expression";
        }

        public bool IsValid() {
            return !string.IsNullOrEmpty(Name)
                && !string.IsNullOrEmpty(Abbr)
                && Abbr.Length == 3
                && Min < Max
                && Min <= DefaultValue
                && DefaultValue <= Max;
        }

        public UExpressionDescriptor Build() {
            return new UExpressionDescriptor(Name, Abbr.ToLower(), Min, Max, DefaultValue, Flag);
        }
    }

    /// <summary>
    /// Interaction logic for ExpressionsDialog.xaml
    /// </summary>
    public partial class ExpressionsDialog : Window, INotifyPropertyChanged {
        public ObservableCollection<ExpressionBuilder> Builders { get; set; }
        public ExpressionBuilder Selected { get; set; }
        public bool IsFlag { get; set; }

        public ExpressionsDialog() {
            InitializeComponent();
            DataContext = this;
            Builders = new ObservableCollection<ExpressionBuilder>(DocManager.Inst.Project.expressions.Select(pair => new ExpressionBuilder(pair.Value)));
            expList.ItemsSource = Builders;
            expList.SelectedIndex = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
        }

        private void Apply() {
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new ConfigureExpressionsCommand(DocManager.Inst.Project, Builders.Select(builder => builder.Build()).ToArray()));
            DocManager.Inst.EndUndoGroup();
        }

        private void expList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Selected = Builders[expList.SelectedIndex];
            IsFlag = Selected.Flag != '\0';
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Selected)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsFlag)));
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e) {
            if (Builders.All(builder => builder.IsValid())) {
                Apply();
                Close();
            } else {
                expList.SelectedItem = Builders.First(builder => !builder.IsValid());
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            Builders.Add(new ExpressionBuilder());
            expList.Items.Refresh();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e) {
            int index = expList.SelectedIndex;
            expList.SelectedIndex = 0;
            Builders.RemoveAt(index);
            expList.Items.Refresh();
        }
    }
}
