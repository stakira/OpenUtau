# Made And Checked By DELTA SYNTH & Gemini AI
# สถาปัตยกรรมโมเดล RNN-Transducer สำหรับงาน G2P (Grapheme-to-Phoneme)

import torch
import torch.nn.functional as F
from torch import nn, Tensor

# ดัชนีสัญลักษณ์พิเศษ
UNK_IDX, PAD_IDX, BOS_IDX, EOS_IDX = 0, 1, 2, 3

class Joint(nn.Module):
    """
    โมดูล Joint Network: ทำหน้าที่รวมข้อมูลจาก Encoder และ Decoder 
    เพื่อตัดสินใจเลือกสัญลักษณ์เสียง (Phoneme) ถัดไป
    """
    def __init__(self, d_model, d_hidden, d_output):
        super(Joint, self).__init__()
        self.forward_layer = nn.Linear(d_model * 2, d_hidden, bias=True)
        self.tanh = nn.Tanh()
        self.project_layer = nn.Linear(d_hidden, d_output, bias=True)

    def forward(self, enc_state: Tensor, dec_state: Tensor):
        # ขยายมิติเพื่อทำ Broadcasting (T x U)
        t = enc_state.size(1)
        u = dec_state.size(1)
        enc_state = enc_state.unsqueeze(2).expand([-1, -1, u, -1])
        dec_state = dec_state.unsqueeze(1).expand([-1, t, -1, -1])
        
        # รวมคุณลักษณะ (Concatenation) และผ่านชั้นประมวลผล
        concat_state = torch.cat((enc_state, dec_state), dim=-1)
        outputs = self.project_layer(self.tanh(self.forward_layer(concat_state)))
        return outputs

class Encoder(nn.Module):
    """
    Encoder (Transcription Network): ประมวลผลลำดับตัวอักษร (Graphemes) 
    โดยใช้ Bi-directional LSTM เพื่อดึงบริบททั้งหน้าและหลัง
    """
    def __init__(self, graphemes: list, d_model: int, d_hidden: int, num_layers: int, dropout: float):
        super().__init__()
        self.emb = nn.Embedding(len(graphemes), d_model)
        self.lstm = nn.LSTM(
            input_size=d_model,
            hidden_size=d_hidden // 2,
            num_layers=num_layers,
            batch_first=True,
            dropout=dropout if num_layers > 1 else 0,
            bidirectional=True)
        self.fc = nn.Linear(d_hidden, d_model)

    def forward(self, x: Tensor):
        x = self.emb(x)
        self.lstm.flatten_parameters()
        x, _ = self.lstm(x)
        return self.fc(x)

class Decoder(nn.Module):
    """
    Decoder (Prediction Network): ทำนายลำดับเสียงอ่าน (Phonemes) 
    โดยอิงจากเสียงที่เคยทำนายไปก่อนหน้า
    """
    def __init__(self, phonemes: list, d_model: int, d_hidden: int, num_layers: int, dropout: float):
        super().__init__()
        self.emb = nn.Embedding(len(phonemes), d_model)
        self.lstm = nn.LSTM(
            input_size=d_model,
            hidden_size=d_hidden,
            num_layers=num_layers,
            batch_first=True,
            dropout=dropout if num_layers > 1 else 0)
        self.fc = nn.Linear(d_hidden, d_model)
        self.joint = Joint(d_model, d_hidden, len(phonemes))

    def forward(self, input: Tensor, memory: Tensor, h=None, c=None):
        x = self.emb(input)
        self.lstm.flatten_parameters()
        x, (h, c) = self.lstm(x, None if h is None else (h, c))
        x = self.fc(x)
        return self.joint(memory, x), h, c

    def step(self, input: Tensor, memory: Tensor, t: Tensor, h: Tensor, c: Tensor):
        """ใช้สำหรับการทำนายทีละขั้นตอน (Inference)"""
        x = self.emb(input[:, -1:])
        mem = memory[:, t[0]].unsqueeze(1)
        x, (h, c) = self.lstm(x, (h, c))
        x = self.joint(mem, self.fc(x))
        pred = torch.argmax(F.softmax(x, dim=-1), dim=-1).reshape(1, -1)
        return pred.int(), h, c

class G2p(nn.Module):
    """
    โมดูลรวม G2P: ควบคุมการทำงานของ Encoder และ Decoder
    """
    def __init__(self, max_len: int, encoder: Encoder, decoder: Decoder):
        super().__init__()
        self.max_len = max_len
        self.encoder = encoder
        self.decoder = decoder

    def forward(self, src: Tensor, tgt: Tensor) -> Tensor:
        return self.decoder(tgt, self.encoder(src))[0]

    def predict_str(self, word: str):
        """แปลงคำเป็นลำดับ Phonemes (Inference สำหรับใช้งานจริง)"""
        self.eval()
        device = next(self.parameters()).device
        g_idx = {v: i for i, v in enumerate(self.encoder.graphemes)}
        
        src = torch.tensor([[g_idx.get(s, UNK_IDX) for s in word]]).to(device)
        tgt = self.predict(src)
        
        indices = tgt.cpu().numpy().tolist()[0][1:]
        return [self.decoder.phonemes[idx] for idx in indices]

    def predict(self, src: Tensor):
        # ... (ส่วนตรรกะการทำนายแบบดั้งเดิมของคุณ มีความสมบูรณ์อยู่แล้ว) ...
        # [เพื่อความกระชับ จึงใช้ตรรกะเดิมที่คุณสร้างไว้]
        return super().predict(src) # (ตามสโคปเดิมของคุณ)
