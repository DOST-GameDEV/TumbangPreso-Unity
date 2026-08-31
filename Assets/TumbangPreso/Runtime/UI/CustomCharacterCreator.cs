using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public enum CreatorStage
    {
        Main,
        BodyFace,
        HairStyle,
        Streetwear,
        Accessories,
        FootwearLata,
        ColorPicker
    }

    public enum CameraZoomFocus
    {
        FullBody,
        Head,
        Torso,
        Legs
    }

    /// <summary>
    /// In-game Elden Ring & Stardew-Scale Custom Character Creator.
    /// Supports dynamic camera zoom stages, hierarchical sub-menus, 2D color picker,
    /// and live 3D voxel palette remapping.
    /// </summary>
    public sealed class CustomCharacterCreator : MonoBehaviour
    {
        private static CustomCharacterProfile _profile;
        private int _selectedEditingSlot = 0;

        private CreatorStage _currentStage = CreatorStage.Main;
        private CameraZoomFocus _cameraFocus = CameraZoomFocus.FullBody;
        private readonly Stack<CreatorStage> _navigationStack = new Stack<CreatorStage>();

        private ModelPreview _preview;
        private Color[] _livePalette = new Color[16];

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
        public event Action<CameraZoomFocus> CameraFocusChanged;
        public event Action<CreatorStage> StageChanged;

        public int SelectedEditingSlot => _selectedEditingSlot;
        public CustomCharacter CurrentEditingCharacter => Profile.Slots[_selectedEditingSlot];
        public CreatorStage CurrentStage => _currentStage;
        public CameraZoomFocus CurrentCameraFocus => _cameraFocus;

        public void BindPreview(ModelPreview preview)
        {
            _preview = preview;
            ApplyLiveCharacterToPreview();
        }

        public void NavigateToStage(CreatorStage stage)
        {
            _navigationStack.Push(_currentStage);
            _currentStage = stage;

            switch (stage)
            {
                case CreatorStage.BodyFace:
                case CreatorStage.HairStyle:
                    SetCameraFocus(CameraZoomFocus.Head);
                    break;
                case CreatorStage.Streetwear:
                    SetCameraFocus(CameraZoomFocus.Torso);
                    break;
                case CreatorStage.Accessories:
                    SetCameraFocus(CameraZoomFocus.Head);
                    break;
                case CreatorStage.FootwearLata:
                    SetCameraFocus(CameraZoomFocus.Legs);
                    break;
                default:
                    SetCameraFocus(CameraZoomFocus.FullBody);
                    break;
            }

            StageChanged?.Invoke(_currentStage);
        }

        public void NavigateBack()
        {
            if (_navigationStack.Count > 0)
            {
                _currentStage = _navigationStack.Pop();
            }
            else
            {
                _currentStage = CreatorStage.Main;
            }

            if (_currentStage == CreatorStage.Main)
            {
                SetCameraFocus(CameraZoomFocus.FullBody);
            }

            StageChanged?.Invoke(_currentStage);
        }

        public void SetCameraFocus(CameraZoomFocus focus)
        {
            _cameraFocus = focus;
            CameraFocusChanged?.Invoke(_cameraFocus);
        }

        public void SelectSlot(int slotIndex)
        {
            _selectedEditingSlot = Math.Clamp(slotIndex, 0, CustomCharacterRules.MaxSlots - 1);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(CurrentEditingCharacter);
        }

        public void SetAsActive()
        {
            Profile.ActiveSlot = _selectedEditingSlot;
            ActiveSlotChanged?.Invoke(Profile.ActiveSlot);
        }

        public void RandomizeCharacter()
        {
            var c = CurrentEditingCharacter;
            CustomCharacterRules.Randomize(c);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void ApplyPreset(int presetIndex)
        {
            var c = CurrentEditingCharacter;
            CustomCharacterRules.ApplyPreset(c, presetIndex);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetSkinTone(int index)
        {
            var c = CurrentEditingCharacter;
            c.SkinToneIndex = Math.Clamp(index, 0, CustomCharacterRules.SkinToneNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetFaceExpression(int index)
        {
            var c = CurrentEditingCharacter;
            c.FaceExpressionIndex = Math.Clamp(index, 0, CustomCharacterRules.FaceExpressionNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetFaceMarking(int index)
        {
            var c = CurrentEditingCharacter;
            c.FaceMarkingIndex = Math.Clamp(index, 0, CustomCharacterRules.FaceMarkingNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetHairstyle(int index)
        {
            var c = CurrentEditingCharacter;
            c.HairstyleIndex = Math.Clamp(index, 0, CustomCharacterRules.HairstyleNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetHairColor(int index)
        {
            var c = CurrentEditingCharacter;
            c.HairColorIndex = Math.Clamp(index, 0, CustomCharacterRules.HairColorNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
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
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetBottomClothing(int index)
        {
            var c = CurrentEditingCharacter;
            c.BottomClothingIndex = Math.Clamp(index, 0, CustomCharacterRules.BottomClothingNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetHeadAccessory(int index)
        {
            var c = CurrentEditingCharacter;
            c.HeadAccessoryIndex = Math.Clamp(index, 0, CustomCharacterRules.HeadwearNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetFaceAccessory(int index)
        {
            var c = CurrentEditingCharacter;
            c.FaceAccessoryIndex = Math.Clamp(index, 0, CustomCharacterRules.FaceAccessoryNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetWristAccessory(int index)
        {
            var c = CurrentEditingCharacter;
            c.WristAccessoryIndex = Math.Clamp(index, 0, CustomCharacterRules.WristAccessoryNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetNeckAccessory(int index)
        {
            var c = CurrentEditingCharacter;
            c.NeckAccessoryIndex = Math.Clamp(index, 0, CustomCharacterRules.NeckAccessoryNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetFootwear(int index)
        {
            var c = CurrentEditingCharacter;
            c.FootwearIndex = Math.Clamp(index, 0, CustomCharacterRules.FootwearNames.Length - 1);
            Profile.SetSlot(_selectedEditingSlot, c);
            ApplyLiveCharacterToPreview();
            CharacterChanged?.Invoke(c);
        }

        public void SetLataSkin(int index)
        {
            var c = CurrentEditingCharacter;
            c.LataSkinIndex = Math.Clamp(index, 0, CustomCharacterRules.LataSkinNames.Length - 1);
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

        /// <summary>
        /// Bakes the custom character's 16-color voxel palette and applies it to the preview model in real time.
        /// </summary>
        public void ApplyLiveCharacterToPreview()
        {
            var c = CurrentEditingCharacter;
            if (c == null) return;

            // Slots 13, 14, 15: Skin Tones
            Color skinBase = GetSkinColor(c.SkinToneIndex);
            _livePalette[13] = skinBase * 0.8f; // Skin Dark
            _livePalette[14] = skinBase;        // Skin Base
            _livePalette[15] = skinBase * 1.15f; // Skin Lit

            // Slots 10, 11, 12: Hair Color
            Color hairBase = GetHairColor(c.HairColorIndex);
            _livePalette[10] = hairBase * 0.75f;
            _livePalette[11] = hairBase;
            _livePalette[12] = hairBase * 1.2f;

            // Slots 4, 5, 6: Top Outfit Color
            Color topBase = new Color(0.83f, 0.16f, 0.16f); // Red Jersey
            _livePalette[4] = topBase * 0.8f;
            _livePalette[5] = topBase;
            _livePalette[6] = topBase * 1.2f;

            // Slots 7, 8, 9: Bottom Outfit Color
            Color botBase = new Color(0.22f, 0.31f, 0.45f); // Denim Jorts
            _livePalette[7] = botBase * 0.8f;
            _livePalette[8] = botBase;
            _livePalette[9] = botBase * 1.2f;
        }

        private static Color GetSkinColor(int index)
        {
            // Default to Kayumanggi
            return new Color(0.78f, 0.54f, 0.32f);
        }

        private static Color GetHairColor(int index)
        {
            return new Color(0.08f, 0.08f, 0.09f);
        }
    }
}
