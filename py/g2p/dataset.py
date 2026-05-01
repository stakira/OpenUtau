# Made And Checked By DELTA SYNTH & Gemini AI
# ส่วนประกอบสำหรับการโหลดข้อมูล Grapheme และ Phoneme สำหรับ DiffSinger AI

import re
import torch
from torch.utils.data import Dataset

# กำหนดดัชนีสำหรับสัญลักษณ์พิเศษที่เป็นมาตรฐาน
UNK_IDX, PAD_IDX, BOS_IDX, EOS_IDX = 0, 1, 2, 3
special_symbols = ['<unk>', '<pad>', '<bos>', '<eos>']

class SphinxDataset(Dataset):
    """
    คลาสสำหรับจัดการชุดข้อมูลพจนานุกรมเสียง (Phonetic Dictionary)
    รองรับการทำความสะอาดข้อมูลและการแปลงเป็น Tensor สำหรับการฝึกสอน AI
    """
    def __init__(self, path, cfg,
                 comment_prefix=';;;',
                 remove_word_digits=True,
                 remove_phoneme_digits=True):
        
        # ตั้งค่า Graphemes (ตัวอักษร) จาก Configuration
        self.graphemes = cfg['encoder']['graphemes']
        self._validate_symbols(self.graphemes, "graphemes")
        self.grapheme_indexs = {v: i for i, v in enumerate(self.graphemes)}

        # ตั้งค่า Phonemes (สัญลักษณ์เสียง) จาก Configuration
        self.phonemes = cfg['decoder']['phonemes']
        self._validate_symbols(self.phonemes, "phonemes")
        self.phoneme_indexs = {v: i for i, v in enumerate(self.phonemes)}

        # โหลดและประมวลผลไฟล์พจนานุกรม
        self.entries = self.load_dict(
            path, comment_prefix, remove_word_digits, remove_phoneme_digits)

    def _validate_symbols(self, symbols, name):
        """ตรวจสอบความถูกต้องของสัญลักษณ์พิเศษในลำดับแรกๆ"""
        assert len(symbols) > 4, f"จำนวน {name} น้อยเกินไป"
        for i, sym in enumerate(special_symbols):
            assert symbols[i] == sym, f"{name}[{i}] ต้องเป็น {sym}"

    def __len__(self):
        return len(self.entries)

    def __getitem__(self, idx):
        if torch.is_tensor(idx):
            idx = idx.tolist()
            
        src_raw, tgt_raw = self.entries[idx]
        
        # แปลงตัวอักษรและเสียงเป็นตัวเลข (Indices)
        # หากไม่พบสัญลักษณ์จะถูกแทนด้วย UNK_IDX (0)
        src = torch.tensor([self.grapheme_indexs.get(s, UNK_IDX)
                           for s in src_raw], dtype=torch.long)
        tgt = torch.tensor([self.phoneme_indexs.get(p, UNK_IDX)
                           for p in tgt_raw], dtype=torch.long)
        
        return src, tgt

    def load_dict(self, path, comment_prefix,
                  remove_word_digits,
                  remove_phoneme_digits):
        """
        โหลดพจนานุกรมจากไฟล์และทำการ Pre-process ข้อมูล
        """
        # Regex สำหรับกำจัดเลขลำดับหลังคำหรือเสียง เช่น WORD(1) หรือ AA1 -> AA
        rm_digit = re.compile(r'\(\d+\)$|\d+$')
        entries = []
        ignored_graphemes = set()
        ignored_phonemes = set()

        try:
            with open(path, 'r', encoding='utf8') as f:
                for line in f:
                    line = line.strip()
                    if not line or line.startswith(comment_prefix):
                        continue

                    parts = line.split()
                    if len(parts) < 2:
                        continue

                    word = parts[0]
                    pron = parts[1:]

                    # ทำความสะอาดข้อมูลเลขต่อท้าย
                    if remove_word_digits:
                        word = rm_digit.sub('', word)
                    if remove_phoneme_digits:
                        pron = [rm_digit.sub('', p) for p in pron]

                    # ตรวจสอบสัญลักษณ์ที่ไม่ได้อยู่ในชุดที่กำหนด (เพื่อเก็บ Log)
                    for c in word:
                        if c not in self.grapheme_indexs:
                            ignored_graphemes.add(c)
                    for p in pron:
                        if p not in self.phoneme_indexs:
                            ignored_phonemes.add(p)
                    
                    entries.append((word, pron))
        except FileNotFoundError:
            print(f"[ข้อผิดพลาด] ไม่พบไฟล์พจนานุกรมที่: {path}")

        # แสดงสรุปผลการโหลดข้อมูล
        print(f"--- สรุปการโหลดข้อมูลพจนานุกรม ---")
        print(f"จำนวนข้อมูลทั้งหมด: {len(entries)} รายการ")
        if ignored_graphemes:
            print(f"อักขระที่ถูกละเว้น: {', '.join(sorted(ignored_graphemes))}")
        if ignored_phonemes:
            print(f"สัญลักษณ์เสียงที่ถูกละเว้น: {', '.join(sorted(ignored_phonemes))}")
            
        return entries
