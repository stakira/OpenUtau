#include "timing.h"

#include <algorithm>
#include <iostream>
#include <vector>

#include "gtest/gtest.h"
#include "worldline/model/model.h"
#include "worldline/model/model_utils.h"
#include "worldline/synth_request.h"

namespace {

TEST(TimingTest, NoStretch) {
  std::vector<double> samples(44100, 0);
  EXPECT_EQ(samples.size(), 44100);
  worldline::Model model(std::move(samples), 44100, 5, nullptr);

  SynthRequest request;
  request.con_vel = 100;
  request.offset = 100;
  request.required_length = 500;
  request.consonant = 100;
  request.cut_off = 100;

  auto mapping = worldline::GetTimeMapping(model, request);

  std::vector<double> expected;
  for (int i = 0; i <= 500; i += 5) {
    expected.push_back(100 + i);
  }
  EXPECT_EQ(expected, mapping);
}

TEST(TimingTest, VowStretch) {
  std::vector<double> samples(44100, 0);
  EXPECT_EQ(samples.size(), 44100);
  worldline::Model model(std::move(samples), 44100, 5, nullptr);

  SynthRequest request;
  request.con_vel = 100;
  request.offset = 100;
  request.required_length = 500;
  request.consonant = 100;
  request.cut_off = -200;  // vowel = 100

  auto mapping = worldline::GetTimeMapping(model, request);

  EXPECT_EQ(mapping.size(), 101);
  EXPECT_EQ(mapping[0], 100);
  EXPECT_EQ(mapping[20], 200);
  EXPECT_GT(mapping[100], 300);
  EXPECT_LE(mapping[100], 305);
}

TEST(TimingTest, ConStretch) {
  std::vector<double> samples(44100, 0);
  EXPECT_EQ(samples.size(), 44100);
  worldline::Model model(std::move(samples), 44100, 5, nullptr);

  SynthRequest request;
  request.con_vel = 50;
  request.offset = 100;
  request.required_length = 500;
  request.consonant = 100;
  request.cut_off = 100;

  auto mapping = worldline::GetTimeMapping(model, request);

  EXPECT_EQ(mapping.size(), 101);
  EXPECT_EQ(mapping[0], 100);
  EXPECT_GT(mapping[20], 170);
  EXPECT_LE(mapping[20], 171);
  EXPECT_LE(mapping[28], 200);
  EXPECT_GT(mapping[29], 200);
  EXPECT_LE(mapping[98], 200 + 70 * 5);
  EXPECT_GT(mapping[99], 200 + 70 * 5);
}

TEST(TimingTest, ConCompressVowStretch) {
  std::vector<double> samples(44100, 0);
  EXPECT_EQ(samples.size(), 44100);
  worldline::Model model(std::move(samples), 44100, 5, nullptr);

  SynthRequest request;
  request.con_vel = 150;
  request.offset = 100;
  request.required_length = 500;
  request.consonant = 100;
  request.cut_off = -200;  // vowel = 100

  auto mapping = worldline::GetTimeMapping(model, request);

  EXPECT_EQ(mapping.size(), 101);
  EXPECT_EQ(mapping[0], 100);
  EXPECT_LE(mapping[14], 200);
  EXPECT_GT(mapping[15], 200);
  EXPECT_GT(mapping[100], 300);
  EXPECT_LE(mapping[100], 305);
}

}  // namespace
