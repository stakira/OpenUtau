using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Controls {
    public partial class ExpSelector : UserControl {
        public static readonly DirectProperty<ExpSelector, int> IndexProperty =
            AvaloniaProperty.RegisterDirect<ExpSelector, int>(
                nameof(Index),
                o => o.Index,
                (o, v) => o.Index = v);

        public int Index {
            get => index;
            set => SetAndRaise(IndexProperty, ref index, value);
        }

        private int index;

        public ExpSelector() {
            InitializeComponent();
            DataContext = new ExpSelectorViewModel();
            ((ExpSelectorViewModel)DataContext!).Index = Index;
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (change.Property == IndexProperty) {
                ((ExpSelectorViewModel)DataContext!).Index = Index;
            }
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        private void TextBlockPointerPressed(object sender, PointerPressedEventArgs e) {
            ((ExpSelectorViewModel)DataContext!).OnSelected();
        }
    }
}
