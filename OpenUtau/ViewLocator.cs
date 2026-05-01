// Made And Checked By DELTA SYNTH & Gemini AI
// ต้นฉบับโดย OpenUtau Team (https://github.com/stakira/OpenUtau)

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App {
    /// <summary>
    /// ระบบระบุตำแหน่งและจับคู่หน้าจอ (View) กับข้อมูลควบคุม (ViewModel) อัตโนมัติ
    /// </summary>
    public class ViewLocator : IDataTemplate {
        /// <summary>
        /// ทำหน้าที่สร้าง Instance ของหน้าจอขึ้นมาเมื่อมีการเรียกใช้ข้อมูลที่เกี่ยวข้อง
        /// </summary>
        /// <param name="data">อ็อบเจกต์ข้อมูล (ViewModel) ที่ต้องการแสดงผล</param>
        /// <returns>หน้าจอ Control ที่จับคู่สำเร็จ หรือข้อความแจ้งเตือนหากไม่พบ</returns>
        public Control? Build(object? data) {
            if (data is null) {
                return null;
            }

            // แปลงชื่อจาก ViewModel เป็น View โดยอัตโนมัติ
            // ตัวอย่าง: MainViewModel -> MainView
            var name = data.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            if (type != null) {
                // สร้างหน้าจอจากประเภทที่ค้นพบ
                return (Control)Activator.CreateInstance(type)!;
            }

            // แสดงข้อความแจ้งเตือนเป็นภาษาไทยกรณีระบุตำแหน่งไม่สำเร็จ
            return new TextBlock { 
                Text = "ไม่พบหน้าจอที่ระบุ: " + name,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
        }

        /// <summary>
        /// ตรวจสอบว่าข้อมูลที่ส่งมานั้นเป็นคลาสพื้นฐานของ ViewModel หรือไม่
        /// </summary>
        public bool Match(object? data) {
            return data is ViewModelBase;
        }
    }
}
