using Microsoft.ML.OnnxRuntime;

namespace OpenUtau.Core {
    public class Onnx {
        public static InferenceSession getInferenceSession(byte[] model) {
            SessionOptions options = new SessionOptions();
            //TODO:设置是否使用directml
            options.AppendExecutionProvider_DML(1);
            return new InferenceSession(model,options);
        }
    }
}
