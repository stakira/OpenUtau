using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenUtau.Core {
    public static class Yaml {
        public static ISerializer DefaultSerializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .WithEventEmitter(next => new FlowEmitter(next))
            .DisableAliases()
            .WithQuotingNecessaryStrings()
            .Build();

        public static IDeserializer DefaultDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public class FlowEmitter : ChainedEventEmitter {
        public FlowEmitter(IEventEmitter nextEmitter) : base(nextEmitter) { }
        public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter) {
            if (eventInfo.Source.Type == typeof(PitchPoint) ||
                eventInfo.Source.Type == typeof(UVibrato) ||
                eventInfo.Source.Type == typeof(UExpression)) {
                eventInfo.Style = MappingStyle.Flow;
            }
            base.Emit(eventInfo, emitter);
        }
        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter) {
            base.Emit(eventInfo, emitter);
        }
    }
}
