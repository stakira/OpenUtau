// ตัวอย่างการปรับแต่งค่า Vibrato เริ่มต้นเพื่อให้เสียงสมูทและเร็วขึ้น
public UVibrato Clone() {
    return new UVibrato {
        length = this.length,
        period = 120.0f,  // [แก้ไข] ลดค่าลงจากเดิม (เช่น 175) เพื่อให้คลื่นสั่นเร็วขึ้น
        depth = 25.0f,    // [แก้ไข] ปรับความลึกให้พอดีกับความเร็ว เพื่อคงความไพเราะ
        @in = this.@in,
        @out = this.@out,
        shift = this.shift,
        drift = this.drift,
        volLink = this.volLink
    };
}
