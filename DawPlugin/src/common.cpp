#include "common.hpp"
#include "choc/memory/choc_Base64.h"
#include "choc/text/choc_JSON.h"
#include "gzip/decompress.hpp"

std::vector<uint8_t> Utils::gunzip(const char *data, size_t size) {
  std::vector<uint8_t> decompressed;
  gzip::Decompressor decompressor;
  decompressor.decompress(decompressed, data, size);
  return decompressed;
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

std::string
Structures::serializeTrackNames(const std::vector<std::string> &trackNames) {
  choc::value::Value value = choc::value::createEmptyArray();
  for (const auto &track : trackNames) {
    value.addArrayElement(track);
  }
  auto json = choc::json::toString(value);
  return choc::base64::encodeToString(json.data(), json.size());
}
std::vector<std::string>
Structures::deserializeTrackNames(const std::string &data) {
  auto value = choc::json::parse(Utils::unBase64ToString(data));
  std::vector<std::string> trackNames;
  for (auto element : value) {
    trackNames.push_back(std::string(element.getString()));
  }
  return trackNames;
}

std::string Structures::serializeOutputMap(const OutputMap &outputMap) {
  choc::value::Value value = choc::value::createEmptyArray();
  for (const auto &mapping : outputMap) {
    choc::value::Value leftChannel = choc::value::createEmptyArray();
    choc::value::Value rightChannel = choc::value::createEmptyArray();
    for (const auto &channel : mapping.first) {
      leftChannel.addArrayElement(channel);
    }
    for (const auto &channel : mapping.second) {
      rightChannel.addArrayElement(channel);
    }
    value.addArrayElement(leftChannel);
    value.addArrayElement(rightChannel);
  }
  auto json = choc::json::toString(value);
  return json;
}

Structures::OutputMap
Structures::deserializeOutputMap(const std::string &data) {
  auto value = choc::json::parse(data);
  OutputMap outputMap;
  for (uint32_t i = 0; i < value.size(); i += 2) {
    std::vector<bool> leftChannel;
    std::vector<bool> rightChannel;
    for (auto element : value[i]) {
      leftChannel.push_back(element.getBool());
    }
    for (auto element : value[i + 1]) {
      rightChannel.push_back(element.getBool());
    }
    outputMap.push_back({leftChannel, rightChannel});
  }
  return outputMap;
}
