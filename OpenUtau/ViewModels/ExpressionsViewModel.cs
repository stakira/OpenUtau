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
        public bool ShowNumbers => showNumbers.Value;
        public bool IsOptions => isOptions.Value;
        public int SelectedType => selectedType.Value;

        private ObservableAsPropertyHelper<bool> isCustom;
        private ObservableAsPropertyHelper<bool> isNumerical;
        private ObservableAsPropertyHelper<bool> showNumbers;
        private ObservableAsPropertyHelper<bool> isOptions;
        private ObservableAsPropertyHelper<int> selectedType;

        public ExpressionBuilder(UExpressionDescriptor descriptor)
            : this(descriptor.name, descriptor.abbr, descriptor.min, descriptor.max, descriptor.isFlag, descriptor.flag,
                  descriptor.options == null ? string.Empty : string.Join(',', descriptor.options)) {
            ExpressionType = descriptor.type;
            DefaultValue = descriptor.defaultValue;
        }

        public ExpressionBuilder()
            : this("new expression", string.Empty, 0, 100, false, string.Empty, string.Empty) {
        }

        public ExpressionBuilder(string name, string abbr, float min, float max, bool isFlag, string flag, string optionValues) {
            Name = name;
            Abbr = abbr;
            Min = min;
            Max = max;
            IsFlag = isFlag;
            Flag = flag;
            OptionValues = optionValues;

            this.WhenAnyValue(x => x.Abbr)
                .Select(abbr => !Core.Format.Ustx.required.Contains(abbr))
                .ToProperty(this, x => x.IsCustom, out isCustom);
            this.WhenAnyValue(x => x.ExpressionType)
                .Select(type => type == UExpressionType.Numerical)
                .ToProperty(this, x => x.IsNumerical, out isNumerical);
            this.WhenAnyValue(x => x.ExpressionType)
                .Select(type => type == UExpressionType.Numerical || type == UExpressionType.Curve)
                .ToProperty(this, x => x.ShowNumbers, out showNumbers);
            this.WhenAnyValue(x => x.ExpressionType)
                .Select(type => type == UExpressionType.Options)
                .ToProperty(this, x => x.IsOptions, out isOptions);
            this.WhenAnyValue(x => x.ExpressionType)
                .Select(type => (int)type)
                .ToProperty(this, x => x.SelectedType, out selectedType);
        }

        public string[]? Validate() {
            if (string.IsNullOrWhiteSpace(Name)) {
                return new string[] { "Name must be set.", "<translate:errors.expression.name>" };
            }
            if (string.IsNullOrWhiteSpace(Abbr)) {
                return new string[] { "Abbreviation must be set.", "<translate:errors.expression.abbrset>" };
            }
            if (ExpressionType == UExpressionType.Numerical) {
                if (Abbr.Trim().Length < 1 || Abbr.Trim().Length > 4) {
                    return new string[] { "Abbreviation must be between 1 and 4 characters long.", "<translate:errors.expression.abbrlong>" };
                }
                if (Min >= Max) {
                    return new string[] { "Min must be smaller than max.", "<translate:errors.expression.min>" };
                }
                if (DefaultValue < Min || DefaultValue > Max) {
                    return new string[] { "Default value must be between min and max.", "<translate:errors.expression.default>" };
                }
            }
            return null;
        }

        public UExpressionDescriptor Build() {
            switch (ExpressionType) {
                case UExpressionType.Numerical:
                    return new UExpressionDescriptor(Name.Trim(), Abbr.Trim().ToLower(), Min, Max, DefaultValue, Flag);
                case UExpressionType.Options:
                    return new UExpressionDescriptor(Name.Trim(), Abbr.Trim().ToLower(), IsFlag, OptionValues.Split(','));
                case UExpressionType.Curve:
                    return new UExpressionDescriptor(Name.Trim(), Abbr.Trim().ToLower(), Min, Max, DefaultValue) {
                        type = UExpressionType.Curve,
                    };
            }
            throw new Exception("Unexpected expression type");
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
            if (!Expressions.All(builder => builder.Validate() == null)) {
                var invalid = Expressions.First(builder => builder.Validate() != null);
                Expression = invalid;
                string[] validate = invalid.Validate()!;
                throw new MessageCustomizableException(validate[0], validate[1], new ArgumentException(validate[0]), false);
            }
            var abbrs = Expressions.GroupBy(builder => builder.Abbr).Where(g => g.Count() > 1).Select(g => g.Key);
            if (abbrs.Count() > 0) {
                throw new MessageCustomizableException("", $"<translate:errors.expression.abbrunique>: {string.Join(", ", abbrs)}", new ArgumentException($"Abbreviations must be unique: {string.Join(", ", abbrs)}"), false);
            }
            var flags = Expressions.Where(builder => !string.IsNullOrEmpty(builder.Flag)).GroupBy(builder => builder.Flag).Where(g => g.Count() > 1).Select(g => g.Key);
            if (flags.Count() > 0) {
                throw new MessageCustomizableException("", $"<translate:errors.expression.flagunique>: {string.Join(", ", flags)}", new ArgumentException($"Flags must be unique: {string.Join(", ", flags)}"), false);
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

        public void GetSuggestions() {
            foreach (var track in DocManager.Inst.Project.tracks) {
                if (track.RendererSettings.Renderer == null) {
                    continue;
                }
                var suggestions = track.RendererSettings.Renderer.GetSuggestedExpressions(track.Singer, track.RendererSettings);
                if (suggestions == null) {
                    continue;
                }
                foreach (var suggestion in suggestions) {
                    //Add if not already in the list
                    if (!expressionsSource.Any(builder => builder.Abbr == suggestion.abbr)) {
                        expressionsSource.Add(new ExpressionBuilder(suggestion));
                    }
                }
            }
        }
    }
}
