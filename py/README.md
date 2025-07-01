# G2P model

## Training

Example: to train en_us arpabet model.

- Place `cmudict-0.7b` file in `en_us` folder.
- Run commands:
```
cd py
pip install -r requirements.txt
python g2p/train.py
```

To train a different model:

- Create a new folder next to `en_us`. Place your dictionary in it.
- Copies `cfg.yaml` into it and modifies graphemes and phonemes to match your language. The first 4 graphemes and phonemes must be `<unk>`, `<pad>`, `<bos>`, `<eos>`.
- Modify `train.py` to load your config and dictionary.
- You may need to reformat your dictionary so that `SphinxDataset` can load it. Or you can write your own dataset class.
- You will need to tweak batch size and epochs (and maybe other parameters) for best results.

## Packing

A g2p pack zip file contains:
```
dict.txt
g2p.onnx
phones.txt
```

- The `G2pPack` class uses entries from `dict.txt` first.
- If not found, it uses `g2p.onnx` to generate phonemes.
- `phones.txt` allows phonemizers to know which phonemes are vowels and which phonemes to stretch.
---
*For compiling the G2p models read [Compiling-G2p-Mdels-Wiki](https://github.com/stakira/OpenUtau/wiki/Compiling-G2p-Models)*
