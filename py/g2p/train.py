# coding=utf-8
import os
import sys
import torch
import hydra
from omegaconf import OmegaConf


def train(trainer):
    print('training...')
    trainer.train()


def export(trainer, model_path, onnx_path):
    print('exporting model...')
    trainer.model.load_state_dict(torch.load(model_path))
    trainer.model.cpu()
    greedy = GreedyG2p(trainer.model.max_len,
                       trainer.model.encoder, trainer.model.decoder)
    greedy.export(onnx_path)

    print('testing...')
    trainer.test('test_log.txt')


if __name__ == '__main__':
    # An example to train the arpabet model.
    sys.path.append(os.path.abspath('.'))
    from g2p.dataset import SphinxDataset
    from g2p.trainer import G2pTrainer
    from g2p.model import GreedyG2p

    # The config specifying grapheme set and phoneme set.
    cfg = OmegaConf.load('g2p/en_us/cfg.yaml')

    # Loads the dataset.
    # Note that SphinxDataset may not work for your dictionary format.
    dataset = SphinxDataset('g2p/en_us/cmudict-0.7b', cfg,
                            comment_prefix=';;;',
                            # "RECORDS(1)" -> "RECORDS"
                            remove_word_digits=True,
                            # "R EH1 K ER0 D Z" -> "R EH K ER D Z"
                            remove_phoneme_digits=True)

    # Create trainer. You may need to adjust the batch size and epochs.
    trainer = G2pTrainer(
        device=torch.device("cuda" if torch.cuda.is_available() else "cpu"),
        loss_device=torch.device("cpu"),
        model=hydra.utils.instantiate(cfg),
        dataset=dataset,
        batch_size=256,
        epochs=10)

    train(trainer)

    export(trainer, 'g2p-best.ptsd', 'g2p.onnx')
