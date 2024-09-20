#pragma once
#include <string>
#include <vector>

namespace Utils {
std::vector<uint8_t> gunzip(const char *data, size_t size);
std::string unBase64ToString(const std::string &encoded);
std::vector<uint8_t> unBase64ToVector(const std::string &encoded);
} // namespace Utils

namespace Structures {
using OutputMap = std::vector<std::pair<std::vector<bool>, std::vector<bool>>>;
std::string serializeTrackNames(const std::vector<std::string> &trackNames);
std::vector<std::string> deserializeTrackNames(const std::string &data);

std::string serializeOutputMap(const OutputMap &outputMap);
OutputMap deserializeOutputMap(const std::string &data);
} // namespace Structures

namespace Constants {
constexpr uint32_t majorVersion = 0;
constexpr uint32_t minorVersion = 1;
constexpr uint32_t patchVersion = 0;
} // namespace Constants
