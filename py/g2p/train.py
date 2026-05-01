# coding=utf-8
# Made And Checked By DELTA SYNTH & Gemini AI
# สคริปต์หลักสำหรับการฝึกสอน (Training) และส่งออก (Export) โมเดล G2P

import os
import sys
import torch
import hydra
from omegaconf import OmegaConf

# ตรวจสอบตำแหน่งโปรเจกต์เพื่อให้ Import โมดูลภายในได้ถูกต้อง
sys.path.append(os.path.abspath('.'))
from g2p.dataset import SphinxDataset
from g2p.trainer import G2pTrainer
from g2p.model import GreedyG2p

def train_process(trainer):
    """เริ่มกระบวนการฝึกสอนโมเดล"""
    print('--- เริ่มต้นการฝึกสอน (Training) ---')
    try:
        trainer.train()
        print('--- การฝึกสอนเสร็จสมบูรณ์ ---')
    except Exception as e:
        print(f'[ข้อผิดพลาด] ระหว่างการฝึกสอน: {e}')

def export_process(trainer, model_path, onnx_path):
    """โหลดโมเดลที่ดีที่สุดและส่งออกเป็นรูปแบบ ONNX"""
    print(f'--- กำลังส่งออกโมเดลไปยัง: {onnx_path} ---')
    
    if not os.path.exists(model_path):
        print(f'[ข้อผิดพลาด] ไม่พบไฟล์โมเดล: {model_path}')
        return

    # โหลด Weight ของโมเดลและย้ายไปยัง CPU เพื่อการ Export
    trainer.model.load_state_dict(torch.load(model_path, map_location='cpu'))
    trainer.model.eval()
    
    # ใช้ GreedyG2p Wrapper เพื่อให้โครงสร้าง ONNX รองรับการทำงานแบบ Step-by-step
    greedy = GreedyG2p(trainer.model.max_len,
                       trainer.model.encoder, 
                       trainer.model.decoder)
    
    greedy.export(onnx_path)
    print('--- ส่งออกโมเดลสำเร็จ ---')

    # ทำการทดสอบโมเดลหลังส่งออกเพื่อบันทึกประสิทธิภาพ
    print('--- กำลังทดสอบโมเดล (Testing) ---')
    trainer.test('test_log.txt')
    print('บันทึกผลการทดสอบไว้ที่: test_log.txt')

if __name__ == '__main__':
    # 1. โหลดการตั้งค่าจากไฟล์ YAML (Graphemes & Phonemes)
    config_path = 'g2p/en_us/cfg.yaml'
    dict_path = 'g2p/en_us/cmudict-0.7b'
    
    print(f'[ระบบ] กำลังโหลด Config จาก: {config_path}')
    cfg = OmegaConf.load(config_path)

    # 2. เตรียมชุดข้อมูลพจนานุกรม
    # ปรับสมดุลข้อมูล: ลบตัวเลขหลังคำออกเพื่อให้โมเดลเรียนรู้รากศัพท์ที่ชัดเจน
    dataset = SphinxDataset(dict_path, cfg,
                            comment_prefix=';;;',
                            remove_word_digits=True,   # "RECORDS(1)" -> "RECORDS"
                            remove_phoneme_digits=True) # "R EH1" -> "R EH"

    # 3. เตรียมระบบ Trainer
    # ตรวจสอบการใช้งาน GPU (CUDA) อัตโนมัติเพื่อความรวดเร็วในการเทรน
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f'[ระบบ] ใช้งานอุปกรณ์: {device}')

    trainer = G2pTrainer(
        device=device,
        loss_device=torch.device("cpu"), # คำนวณ Loss บน CPU เพื่อประหยัด VRAM
        model=hydra.utils.instantiate(cfg),
        dataset=dataset,
        batch_size=256, # ปรับขนาดตามความแรงของ GPU
        epochs=10)      # จำนวนรอบในการเทรน

    # 4. รันกระบวนการ
    train_process(trainer)
    export_process(trainer, 'g2p-best.ptsd', 'g2p.onnx')
