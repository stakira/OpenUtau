# fmt: off
import sys
import os
sys.path.append(os.path.dirname(__file__))

try:
    import torch
except ModuleNotFoundError:
    print('installing pytorch...')
    from install_torch import pip_install_torch
    pip_install_torch(os.path.join('.', 'python-3.8.10-embed-amd64', 'python.exe'))
    print('installed pytorch')

import numpy as np
import pysptk
import pyworld
import nnsvs
import utaupy
import enulib
from shutil import copy
from hydra.utils import to_absolute_path
from nnmnkwii.frontend import merlin as fe
from nnmnkwii.io import hts
from nnmnkwii.postfilters import merlin_post_filter
from nnmnkwii.preprocessing.f0 import interp1d
from nnsvs.multistream import get_static_stream_sizes, split_streams
from omegaconf import DictConfig, OmegaConf
from enulib.common import set_checkpoint, set_normalization_stat
# fmt: on


def get_paths(path_ust):
    path_ust = os.path.abspath(path_ust)
    tmp_dir = os.path.splitext(path_ust)[0]
    if not os.path.exists(tmp_dir):
        os.makedirs(tmp_dir)
    path_temp_ust = os.path.join(tmp_dir, 'temp.ust')
    path_temp_table = os.path.join(tmp_dir, 'temp.table')
    path_full_score = os.path.join(tmp_dir, 'score.full')
    path_mono_score = os.path.join(tmp_dir, 'score.lab')
    path_full_timelag = os.path.join(tmp_dir, 'timelag.full')
    path_mono_timelag = os.path.join(tmp_dir, 'timelag.lab')
    path_full_duration = os.path.join(tmp_dir, 'duration.full')
    path_mono_duration = os.path.join(tmp_dir, 'duration.lab')
    path_full_timing = os.path.join(tmp_dir, 'timing.full')
    path_mono_timing = os.path.join(tmp_dir, 'timing.lab')
    path_acoustic = os.path.join(tmp_dir, 'acoustic.csv')
    path_f0 = os.path.join(tmp_dir, 'acoustic-f0.npy')
    path_sp = os.path.join(tmp_dir, 'acoustic-sp.npy')
    path_ap = os.path.join(tmp_dir, 'acoustic-ap.npy')

    return path_ust, path_temp_ust, path_temp_table, \
        path_full_score, path_mono_score, \
        path_full_timelag, path_mono_timelag, \
        path_full_duration, path_mono_duration, \
        path_full_timing, path_mono_timing, \
        path_acoustic, path_f0, path_sp, path_ap


def phonemize(path_ust):
    plugin = utaupy.utauplugin.load(path_ust)
    voice_dir = plugin.setting['VoiceDir']
    os.chdir(voice_dir)
    config = DictConfig(OmegaConf.load(
        os.path.join(voice_dir, 'enuconfig.yaml')))

    path_ust, path_temp_ust, path_temp_table, \
        path_full_score, path_mono_score, \
        path_full_timelag, path_mono_timelag, \
        path_full_duration, path_mono_duration, \
        path_full_timing, path_mono_timing, \
        path_acoustic, path_f0, path_sp, path_ap = get_paths(path_ust)

    copy(path_ust, path_temp_ust)
    copy(os.path.join(voice_dir, config.table_path), path_temp_table)

    # score
    enulib.utauplugin2score.utauplugin2score(
        path_temp_ust,
        path_temp_table,
        path_full_score,
        strict_sinsy_style=False
    )
    enulib.common.full2mono(path_full_score, path_mono_score)
    # timelag
    enulib.timelag.score2timelag(
        config,
        path_full_score,
        path_full_timelag
    )
    enulib.common.full2mono(path_full_timelag, path_mono_timelag)
    # duration
    enulib.duration.score2duration(
        config,
        path_full_score,
        path_full_timelag,
        path_full_duration
    )
    enulib.common.full2mono(path_full_duration, path_mono_duration)
    # timing
    enulib.timing.generate_timing_label(
        path_full_score,
        path_full_timelag,
        path_full_duration,
        path_full_timing
    )
    enulib.common.full2mono(path_full_timing, path_mono_timing)


def gen_world_params(
    labels,
    acoustic_features,
    binary_dict,
    numeric_dict,
    stream_sizes,
    has_dynamic_features,
    subphone_features="coarse_coding",
    pitch_idx=None,
    num_windows=3,
    post_filter=True,
    sample_rate=48000,
    frame_period=5,
    relative_f0=True,
    vibrato_scale=1.0,
    vuv_threshold=0.3,
):
    # Apply MLPG if necessary
    if np.any(has_dynamic_features):
        static_stream_sizes = get_static_stream_sizes(
            stream_sizes, has_dynamic_features, num_windows
        )
    else:
        static_stream_sizes = stream_sizes

    # Split multi-stream features
    streams = split_streams(acoustic_features, static_stream_sizes)
    if len(streams) == 4:
        mgc, target_f0, vuv, bap = streams
        vib, vib_flags = None, None
    elif len(streams) == 5:
        # Assuming diff-based vibrato parameters
        mgc, target_f0, vuv, bap, vib = streams
        vib_flags = None
    elif len(streams) == 6:
        # Assuming sine-based vibrato parameters
        mgc, target_f0, vuv, bap, vib, vib_flags = streams
    else:
        raise RuntimeError("Not supported streams")

    # Gen waveform by the WORLD vocodoer
    fftlen = pyworld.get_cheaptrick_fft_size(sample_rate)
    alpha = pysptk.util.mcepalpha(sample_rate)

    if post_filter:
        mgc = merlin_post_filter(mgc, alpha)

    spectrogram = pysptk.mc2sp(mgc, fftlen=fftlen, alpha=alpha)
    aperiodicity = pyworld.decode_aperiodicity(
        bap.astype(np.float64), sample_rate, fftlen
    )

    # fill aperiodicity with ones for unvoiced regions
    aperiodicity[vuv.reshape(-1) < vuv_threshold, :] = 1.0
    # WORLD fails catastrophically for out of range aperiodicity
    aperiodicity = np.clip(aperiodicity, 0.0, 1.0)

    # F0
    if relative_f0:
        diff_lf0 = target_f0
        # need to extract pitch sequence from the musical score
        linguistic_features = fe.linguistic_features(
            labels,
            binary_dict,
            numeric_dict,
            add_frame_features=True,
            subphone_features=subphone_features,
        )
        f0_score = nnsvs.gen._midi_to_hz(
            linguistic_features, pitch_idx, False)[:, None]
        lf0_score = f0_score.copy()
        nonzero_indices = np.nonzero(lf0_score)
        lf0_score[nonzero_indices] = np.log(f0_score[nonzero_indices])
        lf0_score = interp1d(lf0_score, kind="slinear")

        f0 = diff_lf0 + lf0_score
        f0[vuv < vuv_threshold] = 0
        f0[np.nonzero(f0)] = np.exp(f0[np.nonzero(f0)])
    else:
        f0 = target_f0
        f0[vuv < vuv_threshold] = 0
        f0[np.nonzero(f0)] = np.exp(f0[np.nonzero(f0)])

    f0 = f0.flatten().astype(np.float64)
    spectrogram = spectrogram.astype(np.float64)
    aperiodicity = aperiodicity.astype(np.float64)

    return f0, spectrogram, aperiodicity


def acoustic(path_ust):
    plugin = utaupy.utauplugin.load(path_ust)
    voice_dir = plugin.setting['VoiceDir']
    os.chdir(voice_dir)
    config = DictConfig(OmegaConf.load(
        os.path.join(voice_dir, 'enuconfig.yaml')))
    set_checkpoint(config, 'acoustic')
    set_normalization_stat(config, 'acoustic')

    path_ust, path_temp_ust, path_temp_table, \
        path_full_score, path_mono_score, \
        path_full_timelag, path_mono_timelag, \
        path_full_duration, path_mono_duration, \
        path_full_timing, path_mono_timing, \
        path_acoustic, path_f0, path_sp, path_ap = get_paths(path_ust)

    enulib.acoustic.timing2acoustic(
        config, path_full_timing, path_acoustic)

    acoustic_features = np.loadtxt(
        path_acoustic, delimiter=',', dtype=np.float32
    )

    duration_modified_labels = hts.load(path_full_timing).round_()
    question_path = to_absolute_path(config.question_path)
    binary_dict, continuous_dict = hts.load_question_set(
        question_path, append_hat_for_LL=False
    )
    model_config = OmegaConf.load(
        to_absolute_path(config['acoustic'].model_yaml))
    pitch_idx = len(binary_dict) + 1
    f0, sp, ap = gen_world_params(
        duration_modified_labels,
        acoustic_features,
        binary_dict,
        continuous_dict,
        model_config.stream_sizes,
        model_config.has_dynamic_features,
        subphone_features=config.acoustic.subphone_features,
        pitch_idx=pitch_idx,
        num_windows=model_config.num_windows,
        post_filter=config.acoustic.post_filter,
        sample_rate=config.sample_rate,
        frame_period=config.frame_period,
        relative_f0=config.acoustic.relative_f0
    )
    np.save(path_f0, f0)
    np.save(path_sp, sp)
    np.save(path_ap, ap)


if __name__ == '__main__':
    print(f'argv: {sys.argv}')
    if sys.argv[1] != 'acoustic':
        phonemize(sys.argv[2])
    if sys.argv[1] != 'phonemize':
        acoustic(sys.argv[2])
