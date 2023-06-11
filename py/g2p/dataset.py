import re
import torch
from torch.utils.data import Dataset

UNK_IDX, PAD_IDX, BOS_IDX, EOS_IDX = 0, 1, 2, 3
special_symbols = ['<unk>', '<pad>', '<bos>', '<eos>']


class SphinxDataset(Dataset):
    def __init__(self, path, cfg,
                 comment_prefix=';;;',
                 remove_word_digits=True,
                 remove_phoneme_digits=True):
        self.graphemes = cfg['encoder']['graphemes']
        assert len(self.graphemes) > 4
        assert self.graphemes[0] == '<unk>'
        assert self.graphemes[1] == '<pad>'
        assert self.graphemes[2] == '<bos>'
        assert self.graphemes[3] == '<eos>'
        self.grapheme_indexs = {v: i for i, v in enumerate(self.graphemes)}

        self.phonemes = cfg['decoder']['phonemes']
        assert len(self.phonemes) > 4
        assert self.phonemes[0] == '<unk>'
        assert self.phonemes[1] == '<pad>'
        assert self.phonemes[2] == '<bos>'
        assert self.phonemes[3] == '<eos>'
        self.phoneme_indexs = {v: i for i, v in enumerate(self.phonemes)}

        self.entries = self.load_dict(
            path, comment_prefix, remove_word_digits, remove_phoneme_digits)

    def __len__(self):
        return len(self.entries)

    def __getitem__(self, idx):
        if torch.is_tensor(idx):
            idx = idx.tolist()
        src, tgt = self.entries[idx]
        src = torch.tensor([self.grapheme_indexs.get(s, UNK_IDX)
                           for s in src], dtype=torch.int32)
        tgt = torch.tensor([self.phoneme_indexs.get(s, UNK_IDX)
                           for s in tgt], dtype=torch.int32)
        return src, tgt

    def load_dict(self, path, comment_prefix,
                  remove_word_digits,
                  remove_phoneme_digits):
        rm_digit = re.compile(r'\(\d+\)$|\d+$')
        entries = []
        ignored_graphemes = set()
        ignored_phonemes = set()

        with open(path, 'r', encoding='utf8') as f:
            for line in f.readlines():
                line = line.strip()
                if line == '' or line.startswith(comment_prefix):
                    continue

                parts = line.split()
                if len(parts) < 2:
                    continue

                word = parts[0]
                pron = parts[1:]
                if remove_word_digits:
                    word = rm_digit.sub('', word)
                if remove_phoneme_digits:
                    pron = [rm_digit.sub('', p) for p in pron]

                for c in word:
                    if c not in self.grapheme_indexs:
                        ignored_graphemes.add(c)
                for p in pron:
                    if p not in self.phoneme_indexs:
                        ignored_phonemes.add(p)
                entries.append((word, pron))

        print('graphemes: {}'.format(
            ', '.join(self.graphemes)))
        print('ignored graphemes: {}'.format(
            ', '.join(sorted(ignored_graphemes))))
        print('phonemes: {}'.format(
            ', '.join(self.phonemes)))
        print('ignored phonemes: {}'.format(
            ', '.join(sorted(ignored_phonemes))))
        return entries
