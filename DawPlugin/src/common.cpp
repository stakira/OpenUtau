#include "common.hpp"
#include <choc/memory/choc_Base64.h>
#include <choc/text/choc_JSON.h>
#include <vector>
#include <zstd.h>

std::vector<uint8_t> Utils::zstd(const uint8_t *data, size_t size, int level) {
  size_t est_compress_size = ZSTD_compressBound(size);

  std::vector<uint8_t> comp_buffer;
  comp_buffer.resize(est_compress_size);

  auto compress_size =
      ZSTD_compress(comp_buffer.data(), est_compress_size, data, size, level);

  comp_buffer.resize(compress_size);
  comp_buffer.shrink_to_fit();

  return comp_buffer;
}
std::vector<uint8_t> Utils::unzstd(const uint8_t *data, size_t size) {
  auto est_decomp_size = ZSTD_getFrameContentSize(data, size);
  if (est_decomp_size == ZSTD_CONTENTSIZE_ERROR || est_decomp_size == 0) {
    return {};
  }
  if (est_decomp_size == ZSTD_CONTENTSIZE_UNKNOWN) {
    est_decomp_size = ZSTD_DStreamOutSize();
  }

  std::vector<uint8_t> decomp_buffer;
  decomp_buffer.resize(est_decomp_size);

  size_t const decomp_size =
      ZSTD_decompress(decomp_buffer.data(), est_decomp_size, data, size);

  decomp_buffer.resize(decomp_size);
  decomp_buffer.shrink_to_fit();
  return decomp_buffer;
}

std::string Utils::unBase64ToString(const std::string &encoded) {
  std::vector<uint8_t> decoded;
  choc::base64::decodeToContainer(decoded, encoded);
  return std::string(decoded.begin(), decoded.end());
}
std::vector<uint8_t> Utils::unBase64ToVector(const std::string &encoded) {
  std::vector<uint8_t> decoded;
  choc::base64::decodeToContainer(decoded, encoded);
  return decoded;
}

double Utils::dbToMultiplier(double db) {
  return (db <= -24)  ? 0
         : (db < -16) ? std::pow(10, (db * 2 + 16) / 20)
                      : std::pow(10, db / 20);
}

choc::value::Value Structures::Track::serialize() const {
  return choc::value::createObject("", "name", name, "pan", pan, "volume",
                                   volume);
}
Structures::Track
Structures::Track::deserialize(const choc::value::ValueView &value) {
  Track track;
  track.name = value["name"].get<std::string>();
  track.pan = value["pan"].get<float>();
  track.volume = value["volume"].get<float>();
  return track;
}

std::string Structures::serializeTracks(const std::vector<Track> &tracks) {
  choc::value::Value value = choc::value::createEmptyArray();
  for (const auto &track : tracks) {
    value.addArrayElement(track.serialize());
  }
  auto json = choc::json::toString(value);
  return choc::base64::encodeToString(json.data(), json.size());
}
std::vector<Structures::Track>
Structures::deserializeTracks(const std::string &data) {
  auto json = Utils::unBase64ToString(data);
  auto value = choc::json::parse(json);
  std::vector<Track> tracks;
  for (const auto &trackValue : value) {
    tracks.push_back(Track::deserialize(trackValue));
  }
  return tracks;
}

std::string Structures::serializeOutputMap(const OutputMap &outputMap) {
  choc::value::Value value = choc::value::createEmptyArray();
  for (const auto &mapping : outputMap) {
    value.addArrayElement(mapping.first.to_string());
    value.addArrayElement(mapping.second.to_string());
  }
  auto json = choc::json::toString(value);
  return json;
}

Structures::OutputMap
Structures::deserializeOutputMap(const std::string &data) {
  auto value = choc::json::parse(data);
  OutputMap outputMap;
  for (uint32_t i = 0; i < value.size(); i += 2) {
    auto leftChannelString = std::string(value[i].getString());
    auto leftChannel =
        std::bitset<DISTRHO_PLUGIN_NUM_OUTPUTS>(leftChannelString);
    auto rightChannelString = std::string(value[i + 1].getString());
    auto rightChannel =
        std::bitset<DISTRHO_PLUGIN_NUM_OUTPUTS>(rightChannelString);

    outputMap.push_back({leftChannel, rightChannel});
  }
  return outputMap;
}
