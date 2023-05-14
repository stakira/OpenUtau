import torch
import torch.nn.functional as F
from torch import nn, Tensor

UNK_IDX, PAD_IDX, BOS_IDX, EOS_IDX = 0, 1, 2, 3
special_symbols = ['<unk>', '<pad>', '<bos>', '<eos>']


class Joint(nn.Module):
    def __init__(self, d_model, d_hidden, d_output):
        super(Joint, self).__init__()
        self.forward_layer = nn.Linear(d_model * 2, d_hidden, bias=True)
        self.tanh = nn.Tanh()
        self.project_layer = nn.Linear(d_hidden, d_output, bias=True)

    def forward(self, enc_state: Tensor, dec_state: Tensor):
        assert enc_state.dim() == 3
        assert dec_state.dim() == 3
        t = enc_state.size(1)
        u = dec_state.size(1)
        enc_state = enc_state.unsqueeze(2)
        dec_state = dec_state.unsqueeze(1)
        enc_state = enc_state.expand([-1, -1, u, -1])
        dec_state = dec_state.expand([-1, t, -1, -1])
        concat_state = torch.cat((enc_state, dec_state), dim=-1)

        outputs = self.forward_layer(concat_state)
        outputs = self.tanh(outputs)
        outputs = self.project_layer(outputs)
        return outputs


class Encoder(nn.Module):
    def __init__(self,
                 graphemes: list[str],
                 d_model: int,
                 d_hidden: int,
                 num_layers: int,
                 dropout: float) -> None:
        super().__init__()
        self.graphemes = graphemes
        self.d_model = d_model
        self.d_hidden = d_hidden
        self.num_layers = num_layers
        self.dropout = dropout
        self.emb = nn.Embedding(len(graphemes), d_model)
        self.lstm = nn.LSTM(
            input_size=d_model,
            hidden_size=d_hidden // 2,
            num_layers=num_layers,
            batch_first=True,
            dropout=dropout,
            bidirectional=True)
        self.fc = nn.Linear(d_hidden, d_model)

    def forward(self, input: Tensor):
        x = self.emb(input)
        self.lstm.flatten_parameters()
        x, _ = self.lstm(x)
        return self.fc(x)


class Decoder(nn.Module):
    def __init__(self,
                 phonemes: list[str],
                 d_model: int,
                 d_hidden: int,
                 num_layers: int,
                 dropout: float) -> None:
        super().__init__()
        self.phonemes = phonemes
        self.d_model = d_model
        self.d_hidden = d_hidden
        self.num_layers = num_layers
        self.dropout = dropout
        self.emb = nn.Embedding(len(phonemes), d_model)
        self.lstm = nn.LSTM(
            input_size=d_model,
            hidden_size=d_hidden,
            num_layers=num_layers,
            batch_first=True,
            dropout=dropout)
        self.fc = nn.Linear(d_hidden, d_model)
        self.joint = Joint(d_model, d_hidden, len(phonemes))

    def encode_full(self, input: Tensor):
        x = self.emb(input)
        self.lstm.flatten_parameters()
        x, _ = self.lstm(x)
        return self.fc(x)

    def forward(self, input: Tensor, memory: Tensor, h=None, c=None):
        x = self.emb(input)
        self.lstm.flatten_parameters()
        x, (h, c) = self.lstm(x, None if h is None else (h, c))
        x = self.fc(x)
        return self.joint(memory, x), h, c

    def step(self, input: Tensor, memory: Tensor,
             t: Tensor, h: Tensor, c: Tensor):
        x = self.emb(input[:, -1:])
        mem = memory[:, t[0]].unsqueeze(1)
        x, (h, c) = self.lstm(x, (h, c))
        x = self.fc(x)
        x = self.joint(mem, x)
        x = F.softmax(x, dim=-1)
        x = torch.argmax(x, dim=-1).reshape(1, -1)
        x = x.int()
        return x, h, c


class G2p(nn.Module):
    def __init__(self,
                 max_len: int,
                 encoder: Encoder,
                 decoder: Decoder) -> None:
        super().__init__()
        self.max_len = max_len
        self.encoder = encoder
        self.decoder = decoder

    def forward(self, src: Tensor, tgt: Tensor) -> Tensor:
        mem = self.encoder(src)
        x, _, _ = self.decoder(tgt, mem)
        return x

    def predict(self, src: Tensor):
        with torch.no_grad():
            self.eval()
            device = next(self.parameters()).device
            src = src.to(device)
            tgt = torch.tensor([[BOS_IDX]]).to(device)
            mem = self.encoder(src)
            src_length = src.shape[-1]
            t = torch.zeros((1)).int().to(device)
            h = torch.zeros((self.decoder.num_layers, 1,
                            self.decoder.d_hidden)).to(device)
            c = torch.zeros((self.decoder.num_layers, 1,
                            self.decoder.d_hidden)).to(device)
            while t < src_length and tgt.shape[1] < self.max_len:
                pred, new_h, new_c = self.decoder.step(tgt, mem, t, h, c)
                if pred.item() != BOS_IDX:
                    tgt = torch.concat([tgt, pred], dim=-1)
                    h = new_h
                    c = new_c
                else:
                    t[0] += 1
        return tgt

    def predict_str(self, word: str):
        self.grapheme_indexs = {v: i for i,
                                v in enumerate(self.encoder.graphemes)}
        src = torch.tensor(
            [[self.grapheme_indexs.get(s, UNK_IDX) for s in word]])
        tgt = self.predict(src)
        tgt = tgt.cpu().int().numpy().tolist()
        return [self.decoder.phonemes[idx] for idx in tgt[0][1:]]


class GreedyG2p(nn.Module):
    def __init__(self,
                 max_len: int,
                 encoder: Encoder,
                 decoder: Decoder) -> None:
        super().__init__()
        self.max_len = max_len
        self.encoder = encoder
        self.decoder = decoder

    def forward(self, src: Tensor, tgt: Tensor, t: Tensor) -> Tensor:
        mem = self.encoder(src)
        mem = mem[:, t[0]].unsqueeze(1)
        x = self.decoder.encode_full(tgt)
        x = x[:, -1].unsqueeze(1)
        x = self.decoder.joint(mem, x)
        x = F.softmax(x[0, 0], dim=-1)
        pred = torch.argmax(x, dim=-1).int()
        return pred

    def predict(self, src: Tensor) -> Tensor:
        tgt = torch.tensor([[2]], dtype=torch.int32)
        t = torch.tensor([0], dtype=torch.int32)
        while t[0] < src.shape[1] and tgt.shape[1] < self.max_len:
            pred = self.forward(src, tgt, t)
            if pred[0] != BOS_IDX:
                tgt = torch.concat([tgt, pred.unsqueeze(0)], dim=-1)
            else:
                t[0] += 1
        return tgt

    def export(self, path):
        self.eval()
        src = torch.zeros((1, 8)).int()
        tgt = torch.zeros((1, 6)).int()
        t = torch.tensor([0]).int()
        torch.onnx.export(
            self, (src, tgt, t), path,
            input_names=['src', 'tgt', 't'],
            output_names=['pred'],
            dynamic_axes={
                'src': {1: 'T'},
                'tgt': {1: 'U'},
            },
            opset_version=11)
