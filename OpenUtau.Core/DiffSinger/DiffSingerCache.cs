using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using K4os.Hash.xxHash;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Serilog;

namespace OpenUtau.Core.DiffSinger {

    public class DiffSingerCache {
        private const string FormatHeader = "TENSORCACHE";

        private readonly ulong hash;
        private readonly string filename;

        public ulong Hash => hash;

        public DiffSingerCache(ulong identifier, ICollection<NamedOnnxValue> inputs) {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream)) {
                writer.Write(identifier);
                foreach (var onnxValue in inputs.OrderBy(v => v.Name, StringComparer.InvariantCulture)) {
                    SerializeNamedOnnxValue(writer, onnxValue);
                }
            }

            hash = XXH64.DigestOf(stream.ToArray());
            filename = $"ds-{hash:x16}.tensorcache";
        }

        public ICollection<NamedOnnxValue>? Load() {
            var cachePath = Path.Join(PathManager.Inst.CachePath, filename);
            if (!File.Exists(cachePath)) return null;

            var result = new List<NamedOnnxValue>();
            using var stream = new FileStream(cachePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);
            // header
            if (reader.ReadString() != FormatHeader) {
                throw new InvalidDataException($"[TensorCache] Unexpected file header in {filename}.");
            }
            try {
                // count
                var count = reader.ReadInt32();
                for (var i = 0; i < count; ++i) {
                    // data
                    result.Add(DeserializeNamedOnnxValue(reader));
                }
            } catch (Exception e) {
                Log.Error(e,
                    "[TensorCache] Exception encountered when deserializing cache file. Root exception message: {msg}", e.Message);
                Delete();
                return null;
            }

            return result;
        }

        public void Delete() {
            var cachePath = Path.Join(PathManager.Inst.CachePath, filename);
            if (File.Exists(cachePath)) {
                File.Delete(cachePath);
            }
        }

        public void Save(ICollection<NamedOnnxValue> outputs) {
            var cachePath = Path.Join(PathManager.Inst.CachePath, filename);
            using var stream = new FileStream(cachePath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);
            // header
            writer.Write(FormatHeader);
            // count
            writer.Write(outputs.Count);
            foreach (var onnxValue in outputs) {
                // data
                SerializeNamedOnnxValue(writer, onnxValue);
            }
        }

        private static void SerializeNamedOnnxValue(BinaryWriter writer, NamedOnnxValue namedOnnxValue) {
            if (namedOnnxValue.ValueType != OnnxValueType.ONNX_TYPE_TENSOR) {
                throw new NotSupportedException(
                    $"[TensorCache] The only supported ONNX value type is {OnnxValueType.ONNX_TYPE_TENSOR}. Got {namedOnnxValue.ValueType} instead."
                    );
            }
            // name
            writer.Write(namedOnnxValue.Name);
            var tensorBase = (TensorBase) namedOnnxValue.Value;
            var elementType = tensorBase.GetTypeInfo().ElementType;
            // dtype
            writer.Write((int)elementType);
            switch (elementType) {
                case TensorElementType.Float: {
                    var tensor = namedOnnxValue.AsTensor<float>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.UInt8: {
                    var tensor = namedOnnxValue.AsTensor<byte>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.Int8: {
                    var tensor = namedOnnxValue.AsTensor<sbyte>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.UInt16: {
                    var tensor = namedOnnxValue.AsTensor<ushort>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.Int16: {
                    var tensor = namedOnnxValue.AsTensor<short>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.Int32: {
                    var tensor = namedOnnxValue.AsTensor<int>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.Int64: {
                    var tensor = namedOnnxValue.AsTensor<long>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.String: {
                    var tensor = namedOnnxValue.AsTensor<string>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.Bool: {
                    var tensor = namedOnnxValue.AsTensor<bool>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.Float16: {
                    var tensor = namedOnnxValue.AsTensor<Float16>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.Double: {
                    var tensor = namedOnnxValue.AsTensor<double>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.UInt32: {
                    var tensor = namedOnnxValue.AsTensor<uint>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.UInt64: {
                    var tensor = namedOnnxValue.AsTensor<ulong>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.BFloat16: {
                    var tensor = namedOnnxValue.AsTensor<BFloat16>();
                    SerializeTensor(writer, tensor);
                    break;
                }
                case TensorElementType.Complex64:
                case TensorElementType.Complex128:
                case TensorElementType.DataTypeMax:
                default:
                    throw new NotSupportedException($"[TensorCache] Unsupported tensor element type: {elementType}.");
            }
        }

        private static void SerializeTensor<T>(BinaryWriter writer, Tensor<T> tensor) {
            if (tensor.IsReversedStride) {
                throw new NotSupportedException("[TensorCache] Tensors in reversed strides are not supported.");
            }
            // rank
            writer.Write(tensor.Rank);
            // shape
            foreach (var dim in tensor.Dimensions) {
                writer.Write(dim);
            }
            // size
            var size = (int)tensor.Length;
            writer.Write(size);
            if (typeof(T) == typeof(string)) {
                // string tensor
                // data
                foreach (var element in tensor.ToArray()) {
                    writer.Write(element!.ToString());
                }
            } else {
                // numeric tensor
                // data
                var data = new byte[size * tensor.GetTypeInfo().TypeSize];
                Buffer.BlockCopy(tensor.ToArray(), 0, data, 0, data.Length);
                writer.Write(data);
            }
        }

        private static NamedOnnxValue DeserializeNamedOnnxValue(BinaryReader reader) {
            // name
            var name = reader.ReadString();
            // dtype
            var dtype = (TensorElementType)reader.ReadInt32();
            // rank
            var rank = reader.ReadInt32();
            // shape
            int[] shape = new int[rank];
            for (var i = 0; i < rank; ++i) {
                shape[i] = reader.ReadInt32();
            }
            // size
            var size = reader.ReadInt32();
            NamedOnnxValue namedOnnxValue;
            switch (dtype) {
                case TensorElementType.Float: {
                    var tensor = DeserializeTensor<float>(reader, size, sizeof(float), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.UInt8: {
                    var tensor = DeserializeTensor<byte>(reader, size, sizeof(byte), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.Int8: {
                    var tensor = DeserializeTensor<sbyte>(reader, size, sizeof(sbyte), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.UInt16: {
                    var tensor = DeserializeTensor<ushort>(reader, size, sizeof(ushort), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.Int16: {
                    var tensor = DeserializeTensor<short>(reader, size, sizeof(short), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.Int32: {
                    var tensor = DeserializeTensor<int>(reader, size, sizeof(int), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.Int64: {
                    var tensor = DeserializeTensor<long>(reader, size, sizeof(long), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.String: {
                    // string tensor
                    Tensor<string> tensor = new DenseTensor<string>(size);
                    for (var i = 0; i < size; ++i) {
                        tensor[i] = reader.ReadString();
                    }
                    tensor = tensor.Reshape(shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.Bool: {
                    var tensor = DeserializeTensor<bool>(reader, size, sizeof(bool), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.Float16: {
                    var tensor = DeserializeTensor<Float16>(reader, size, sizeof(ushort), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.Double: {
                    var tensor = DeserializeTensor<double>(reader, size, sizeof(double), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.UInt32: {
                    var tensor = DeserializeTensor<uint>(reader, size, sizeof(uint), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.UInt64: {
                    var tensor = DeserializeTensor<ulong>(reader, size, sizeof(ulong), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.BFloat16: {
                    var tensor = DeserializeTensor<BFloat16>(reader, size, sizeof(ushort), shape);
                    namedOnnxValue = NamedOnnxValue.CreateFromTensor(name, tensor);
                    break;
                }
                case TensorElementType.Complex64:
                case TensorElementType.Complex128:
                case TensorElementType.DataTypeMax:
                default:
                    throw new NotSupportedException($"[TensorCache] Unsupported tensor element type: {dtype}.");
            }

            return namedOnnxValue;
        }

        private static Tensor<T> DeserializeTensor<T>(BinaryReader reader, int size, int typeSize, ReadOnlySpan<int> shape)
        {
            var bytes = reader.ReadBytes(size * typeSize);
            var data = new T[size];
            Buffer.BlockCopy(bytes, 0, data, 0, bytes.Length);
            Tensor<T> tensor = new DenseTensor<T>(data, shape);
            return tensor;
        }
    }
}
