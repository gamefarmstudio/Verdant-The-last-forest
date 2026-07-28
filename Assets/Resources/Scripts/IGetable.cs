using UnityEngine;

namespace Verdant.Combat
{
    /// <summary>
    /// อินเทอร์เฟซกลางสำหรับ "ทุกสิ่งที่สามารถเก็บได้" (เช่น ทรัพยากร, ของสะสม ฯลฯ)
    /// ทำให้ระบบเก็บของไม่ผูกกับคลาสใดคลาสหนึ่ง — เปลี่ยนของที่เก็บได้อิสระ
    /// </summary>
    public interface IGetable
    {
        /// <summary>ตำแหน่งของสิ่งที่เก็บได้ (ให้ผู้เล่นเล็งตาม)</summary>
        Transform Transform { get; }
        /// <summary>จำนวนที่เก็บได้ (เช่น 1, 5, 10)</summary>
        int Amount { get; }
        /// <summary>เมื่อผู้เล่นเก็บสิ่งนี้แล้ว</summary>
        void OnCollected();
    }
}