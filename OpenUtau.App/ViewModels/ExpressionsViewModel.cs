using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class ExpressionBuilder : ReactiveObject {
        private static readonly string[] required = { "vel", "vol", "atk", "dec" };

        [Reactive] public string Name { get; set; }
        [Reactive] public string Abbr { get; set; }
        [Reactive] public UExpressionType ExpressionType { get; set; }
        [Reactive] public float Min { get; set; }
        [Reactive] public float Max { get; set; }
        [Reactive] public float DefaultValue { get; set; }
        [Reactive] public bool IsFlag { get; set; }
        [Reactive] public string Flag { get; set; }
        [Reactive] public string OptionValues { get; set; }

        public bool IsCustom => isCustom.Value;
        public bool IsNumerical => isNumerical.Value;
        public bool IsOptions => isOptions.Value;
        public int SelectedType => selectedType.Value;

        private ObservableAsPropertyHelper<bool> isCustom;
        private ObservableAsPropertyHelper<bool> isNumerical;
        private ObservableAsPropertyHelper<bool> isOptions;
        private ObservableAsPropertyHelper<int> selectedType;

        public ExpressionBuilder(UExpressionDescriptor descriptor)
            : this(descriptor.name, descriptor.abbr, descriptor.min, descriptor.max, descriptor.flag,
                  descriptor.options == null ? string.Empty : string.Join(',', descriptor.options)) {
            ExpressionType = descriptor.type;
            DefaultValue = descriptor.defaultValue;
        }

        public ExpressionBuilder()
            : this("new expression", string.Empty, 0, 100, string.Empty, string.Empty) {
        }

        public ExpressionBuilder(string name, string abbr, float min, float max, string flag, string optionValues) {
            Name = name;
            Abbr = abbr;
            Min = min;
            Max = max;
            Flag = flag;
            OptionValues = optionValues;

            this.WhenAnyValue(x => x.Abbr)
            .Select(abbr => !required.Contains(abbr))
            .ToProperty(this, x => x.IsCustom, out isCustom);
            this.WhenAnyValue(x => x.ExpressionType)
                .Select(type => type == UExpressionType.Numerical)
                .ToProperty(this, x => x.IsNumerical, out isNumerical);
            this.WhenAnyValue(x => x.ExpressionType)
                .Select(type => type == UExpressionType.Options)
                .ToProperty(this, x => x.IsOptions, out isOptions);
            this.WhenAnyValue(x => x.ExpressionType)
                .Select(type => (int)type)
                .ToProperty(this, x => x.SelectedType, out selectedType);
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
            return ExpressionType == UExpressionType.Numerical
            ? new UExpressionDescriptor(
                Name.Trim(), Abbr.Trim().ToLower(), Min, Max, DefaultValue, Flag)
            : new UExpressionDescriptor(
                Name.Trim(), Abbr.Trim().ToLower(), IsFlag, OptionValues.Split(','));
        }

        public override string ToString() => Name;
    }

    public class ExpressionsViewModel : ViewModelBase {

        public ReadOnlyObservableCollection<ExpressionBuilder> Expressions => expressions;

        public ExpressionBuilder? Expression {
            get => expression;
            set => this.RaiseAndSetIfChanged(ref expression, value);
        }

        private ReadOnlyObservableCollection<ExpressionBuilder> expressions;
        private ExpressionBuilder? expression;

        private ObservableCollectionExtended<ExpressionBuilder> expressionsSource;

        public ExpressionsViewModel() {
            expressionsSource = new ObservableCollectionExtended<ExpressionBuilder>();
            expressionsSource.ToObservableChangeSet()
                .Bind(out expressions)
                .Subscribe();
            expressionsSource.AddRange(DocManager.Inst.Project.expressions.Select(pair => new ExpressionBuilder(pair.Value)));
            if (expressionsSource.Count > 0) {
                expression = expressionsSource[0];
            }
        }

        public void Apply() {
            if (!Expressions.All(builder => builder.IsValid())) {
                var invalid = Expressions.First(builder => !builder.IsValid());
                Expression = invalid;
                if (string.IsNullOrWhiteSpace(invalid.Name)) {
                    throw new ArgumentException("Name must be set.");
                } else if (string.IsNullOrWhiteSpace(invalid.Abbr)) {
                    throw new ArgumentException("Abbreviation must be set.");
                } else if (invalid.Abbr.Trim().Length != 3) {
                    throw new ArgumentException("Abbreviation must be 3 characters long.");
                } else {
                    throw new ArgumentException("Invalid min, max or default Value.");
                }
            }
            var abbrs = Expressions.Select(builder => builder.Abbr);
            if (abbrs.Count() != abbrs.Distinct().Count()) {
                throw new ArgumentException("Abbreviations must be unique.");
            }
            var flags = Expressions.Where(builder => !string.IsNullOrEmpty(builder.Flag)).Select(builder => builder.Flag);
            if (flags.Count() != flags.Distinct().Count()) {
                throw new ArgumentException("Flags must be unique.");
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new ConfigureExpressionsCommand(DocManager.Inst.Project, Expressions.Select(builder => builder.Build()).ToArray()));
            DocManager.Inst.EndUndoGroup();
        }

        public void Add() {
            var newExpression = new ExpressionBuilder();
            expressionsSource.Add(newExpression);
            Expression = newExpression;
        }

        public void Remove() {
            if (Expression != null) {
                expressionsSource.Remove(Expression);
            }
        }
    }
}
