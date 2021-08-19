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
        public float Max { get; set; } = 100;
        public float DefaultValue { get; set; }
        public string Flag { get; set; }

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
            return !string.IsNullOrWhiteSpace(Name)
                && !string.IsNullOrWhiteSpace(Abbr)
                && Abbr.Trim().Length == 3
                && Min < Max
                && Min <= DefaultValue
                && DefaultValue <= Max;
        }

        public UExpressionDescriptor Build() {
            return new UExpressionDescriptor(Name.Trim(), Abbr.Trim().ToLower(), Min, Max, DefaultValue, Flag);
        }
    }

    /// <summary>
    /// Interaction logic for ExpressionsDialog.xaml
    /// </summary>
    public partial class ExpressionsDialog : Window, INotifyPropertyChanged {
        static readonly string[] kBuiltIns = { "vel", "vol", "acc", "dec" };
        public ObservableCollection<ExpressionBuilder> Builders { get; set; }
        public ExpressionBuilder Selected { get; set; }
        public bool IsEditable { get; set; }

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
            IsEditable = !kBuiltIns.Contains(Selected.Abbr);
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Selected)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsEditable)));
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e) {
            if (Builders.All(builder => builder.IsValid())) {
                var abbrs = Builders.Select(builder => builder.Abbr);
                if (abbrs.Count() != abbrs.Distinct().Count()) {
                    MessageBox.Show("Abbreviations must be unique.", "Error");
                    return;
                }
                var flags = Builders.Where(builder => !string.IsNullOrEmpty(builder.Flag)).Select(builder => builder.Flag);
                if (flags.Count() != flags.Distinct().Count()) {
                    MessageBox.Show("Flags must be unique.", "Error");
                    return;
                }
                Apply();
                Close();
            } else {
                var invalid = Builders.First(builder => !builder.IsValid());
                expList.SelectedItem = invalid;
                if (string.IsNullOrWhiteSpace(invalid.Name)) {
                    MessageBox.Show("Name must be set.", "Error");
                } else if (string.IsNullOrWhiteSpace(invalid.Abbr)) {
                    MessageBox.Show("Abbreviation must be set.", "Error");
                } else if (invalid.Abbr.Trim().Length != 3) {
                    MessageBox.Show("Abbreviation must be 3 characters long.", "Error");
                } else {
                    MessageBox.Show("Invalid min, max or default Value.", "Error");
                }
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
