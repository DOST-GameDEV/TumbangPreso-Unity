using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// UI Controller for the 3-Slot "Create Your Own Character" Custom Character Creator.
    ///
    /// ⚠️ ROSTER INTEGRITY: Canonical heroes (Berto, Sean, Dante, Cheska, Zack, Nemu, Phaister)
    /// maintain locked canonical skin tones and identities. This customizer operates exclusively on
    /// the dedicated 3 player custom character save slots.
    /// </summary>
    public sealed class CustomCharacterCreator : MonoBehaviour
    {
        private static CustomCharacterProfile _profile;
        private int _selectedEditingSlot = 0;

        public static CustomCharacterProfile Profile
        {
            get
            {
                if (_profile == null) _profile = new CustomCharacterProfile();
                return _profile;
            }
            set => _profile = value;
        }

        public event Action<CustomCharacter> CharacterChanged;
        public event Action<int> ActiveSlotChanged;

        public int SelectedEditingSlot => _selectedEditingSlot;
        public CustomCharacter CurrentEditingCharacter => Profile.Slots[_selectedEditingSlot];

        public void SelectSlot(int slotIndex)
        {
            _selectedEditingSlot = Math.Clamp(slotIndex, 0, CustomCharacterRules.MaxSlots - 1);
            CharacterChanged?.Invoke(CurrentEditingCharacter);
        }

        public void SetAsActive()
        {
            Profile.ActiveSlot = _selectedEditingSlot;
            ActiveSlotChanged?.Invoke(Profile.ActiveSlot);
        }

        public void SetSkinTone(int index)
        {
            var c = CurrentEditingCharacter;
            c.SkinToneIndex = Math.Clamp(index, 0, CustomCharacterRules.SkinToneNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetFaceExpression(int index)
        {
            var c = CurrentEditingCharacter;
            c.FaceExpressionIndex = Math.Clamp(index, 0, CustomCharacterRules.FaceExpressionNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetHairstyle(int index)
        {
            var c = CurrentEditingCharacter;
            c.HairstyleIndex = Math.Clamp(index, 0, CustomCharacterRules.HairstyleNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetHairColor(int index)
        {
            var c = CurrentEditingCharacter;
            c.HairColorIndex = Math.Clamp(index, 0, CustomCharacterRules.HairColorNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetHeightPercent(int percent)
        {
            var c = CurrentEditingCharacter;
            c.HeightPercent = Math.Clamp(percent, CustomCharacter.MinHeightPercent, CustomCharacter.MaxHeightPercent);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetBuildSize(int index)
        {
            var c = CurrentEditingCharacter;
            c.BuildSizeIndex = Math.Clamp(index, 0, CustomCharacterRules.BuildSizeNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetTopClothing(int index)
        {
            var c = CurrentEditingCharacter;
            c.TopClothingIndex = Math.Clamp(index, 0, CustomCharacterRules.TopClothingNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetBottomClothing(int index)
        {
            var c = CurrentEditingCharacter;
            c.BottomClothingIndex = Math.Clamp(index, 0, CustomCharacterRules.BottomClothingNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetHeadAccessory(int index)
        {
            var c = CurrentEditingCharacter;
            c.HeadAccessoryIndex = Math.Clamp(index, 0, CustomCharacterRules.HeadAccessoryNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetFaceAccessory(int index)
        {
            var c = CurrentEditingCharacter;
            c.FaceAccessoryIndex = Math.Clamp(index, 0, CustomCharacterRules.FaceAccessoryNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetWristAccessory(int index)
        {
            var c = CurrentEditingCharacter;
            c.WristAccessoryIndex = Math.Clamp(index, 0, CustomCharacterRules.WristAccessoryNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }

        public void SetName(string name)
        {
            var c = CurrentEditingCharacter;
            c.Name = name;
            Profile.SetSlot(_selectedEditingSlot, c);
            CharacterChanged?.Invoke(c);
        }
    }
}
